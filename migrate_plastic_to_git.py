#!/usr/bin/env python3
"""
Migrate Plastic SCM history to Git, preserving all 95 commits and 8-branch structure.
Uses cm cat and cm diff to extract file changes for each changeset.
"""

import xml.etree.ElementTree as ET
import subprocess
import os
import sys
import tempfile
import shutil
from datetime import datetime
from collections import defaultdict

# Configuration
PLASTIC_HISTORY_FILE = "PLASTIC_SCM_HISTORY.xml"
GIT_AUTHOR_EMAIL = "danilodevs@gmail.com"
GIT_AUTHOR_NAME = "Danilo Devs"
WORK_DIR = "E:\\Projects\\Unity\\ClosureBT"

def run_command(cmd, capture=False, cwd=None, check=False):
    """Execute shell command."""
    try:
        if cwd is None:
            cwd = WORK_DIR

        if capture:
            result = subprocess.run(cmd, shell=True, capture_output=True, text=True, cwd=cwd)
            if check and result.returncode != 0:
                raise RuntimeError(f"Command failed: {cmd}\n{result.stderr}")
            return result.stdout.strip(), result.returncode
        else:
            result = subprocess.run(cmd, shell=True, cwd=cwd)
            if check and result.returncode != 0:
                raise RuntimeError(f"Command failed: {cmd}")
            return None, result.returncode
    except Exception as e:
        if check:
            raise
        print(f"Error running command: {cmd}")
        print(f"Exception: {e}")
        return None, 1

def parse_xml_history():
    """Parse Plastic SCM history XML file."""
    tree = ET.parse(os.path.join(WORK_DIR, PLASTIC_HISTORY_FILE))
    root = tree.getroot()

    changesets = []
    for cs_elem in root.findall('Changeset'):
        cs_id = int(cs_elem.find('ChangesetId').text)
        branch = cs_elem.find('Branch').text
        comment = cs_elem.find('Comment').text or ""
        owner = cs_elem.find('Owner').text
        date_str = cs_elem.find('Date').text

        # Get file changes
        changes = []
        for item in cs_elem.find('Changes').findall('Item'):
            change_type = item.find('Type').text
            src_path = item.find('SrcCmPath').text
            dst_path = item.find('DstCmPath').text
            changes.append({
                'type': change_type,
                'src_path': src_path,
                'dst_path': dst_path
            })

        changesets.append({
            'id': cs_id,
            'branch': branch,
            'comment': comment,
            'owner': owner,
            'date_str': date_str,
            'changes': changes
        })

    # Sort by changeset ID (oldest first)
    changesets.sort(key=lambda x: x['id'])
    return changesets

def generate_commit_message(cs):
    """Generate commit message, handling empty messages."""
    if cs['comment'].strip():
        return cs['comment']

    # Generate message from file changes
    change_types = defaultdict(int)
    for change in cs['changes']:
        change_types[change['type']] += 1

    if not change_types:
        return f"Update (changeset {cs['id']})"

    parts = []
    if change_types['Added']:
        parts.append(f"Add {change_types['Added']} file(s)")
    if change_types['Changed']:
        parts.append(f"Update {change_types['Changed']} file(s)")
    if change_types['Deleted']:
        parts.append(f"Delete {change_types['Deleted']} file(s)")
    if change_types['Moved']:
        parts.append(f"Move {change_types['Moved']} file(s)")

    return ", ".join(parts)

def get_or_create_branch(branch_name):
    """Get or create a Git branch."""
    # Convert Plastic branch path to Git branch name
    git_branch = branch_name.lstrip('/').replace('/', '-')

    # Check if branch exists
    cmd = f'git rev-parse --verify {git_branch} 2>nul'
    _, code = run_command(cmd)

    if code != 0:
        # Create branch from current HEAD
        run_command(f'git checkout -b {git_branch}')
        print(f"    Created branch: {git_branch}")
    else:
        run_command(f'git checkout {git_branch}')

    return git_branch

def apply_changeset_files(cs, changeset_id):
    """Apply file changes from a changeset by extracting from Plastic."""
    # For each file in the changeset, get its content at this revision
    files_to_process = []

    for change in cs['changes']:
        change_type = change['type']
        path = change['dst_path']

        if change_type == 'Added' or change_type == 'Changed':
            files_to_process.append(('add_or_update', path, changeset_id))
        elif change_type == 'Deleted':
            files_to_process.append(('delete', path, changeset_id))
        elif change_type == 'Moved':
            # For moves, delete old and add new
            old_path = change['src_path']
            new_path = change['dst_path']
            files_to_process.append(('delete', old_path, changeset_id))
            files_to_process.append(('add_or_update', new_path, changeset_id))

    for action, path, cs_id in files_to_process:
        if action == 'add_or_update':
            # Get file content from Plastic
            cmd = f'cm cat "{path}" --rev="cs:{cs_id}" 2>nul'
            content, code = run_command(cmd, capture=True)

            if code == 0:
                # Create directories if needed
                full_path = os.path.join(WORK_DIR, path.lstrip('/'))
                os.makedirs(os.path.dirname(full_path), exist_ok=True)

                # Write file
                with open(full_path, 'wb') as f:
                    f.write(content.encode('utf-8', errors='ignore') if isinstance(content, str) else content)
            else:
                print(f"      Warning: Could not extract {path}")

        elif action == 'delete':
            full_path = os.path.join(WORK_DIR, path.lstrip('/'))
            if os.path.exists(full_path):
                os.remove(full_path)
                run_command(f'git rm "{path}" 2>nul')

def create_git_commit(message, date_str, author_email, author_name):
    """Create a Git commit with specific date and author."""
    # Stage all changes
    run_command('git add -A', check=False)

    # Format date for Git (ISO 8601)
    timestamp = date_str

    # Create commit with proper author info
    message_safe = message.replace('"', '\\"').replace('$', '\\$')
    commit_cmd = f'git -c user.email="{author_email}" -c user.name="{author_name}" commit --allow-empty --date="{timestamp}" -m "{message_safe}"'

    _, code = run_command(commit_cmd)
    return code == 0

def migrate_history():
    """Migrate all changesets from Plastic to Git."""
    print("Parsing Plastic SCM history...")
    changesets = parse_xml_history()
    print(f"Found {len(changesets)} changesets (IDs: {changesets[0]['id']} to {changesets[-1]['id']})\n")

    # Track which branches we've created
    git_branches = set()
    current_branch = None

    print("Starting migration...\n")

    for i, cs in enumerate(changesets):
        cs_num = i + 1
        branch_display = cs['branch'][:40]
        print(f"[{cs_num:3d}/{len(changesets)}] cs:{cs['id']:3d} | {branch_display:<40} | ", end='', flush=True)

        # Determine Git branch
        git_branch = cs['branch'].lstrip('/').replace('/', '-')

        # Switch or create branch if needed
        if git_branch != current_branch:
            get_or_create_branch(git_branch)
            current_branch = git_branch
            git_branches.add(git_branch)

        # Generate commit message
        message = generate_commit_message(cs)

        # Apply file changes
        apply_changeset_files(cs, cs['id'])

        # Create commit
        if create_git_commit(message, cs['date_str'], GIT_AUTHOR_EMAIL, GIT_AUTHOR_NAME):
            print(f"✓")
        else:
            print(f"✗ commit failed")

    print(f"\n{'='*70}")
    print(f"✓ Migration complete!")
    print(f"  - {len(changesets)} commits created")
    print(f"  - {len(git_branches)} branches created")
    print(f"{'='*70}\n")

    print("Branches created:")
    run_command('git branch -a')

def main():
    """Main entry point."""
    if not os.path.exists(os.path.join(WORK_DIR, PLASTIC_HISTORY_FILE)):
        print(f"Error: {PLASTIC_HISTORY_FILE} not found in {WORK_DIR}")
        sys.exit(1)

    print("=" * 70)
    print("Plastic SCM to Git History Migration")
    print("=" * 70)
    print()

    try:
        migrate_history()
        print("Next steps:")
        print("  1. git log --oneline -20")
        print("  2. git push -u origin --all")
    except Exception as e:
        print(f"\nError during migration: {e}")
        sys.exit(1)

if __name__ == "__main__":
    main()
