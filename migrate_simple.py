#!/usr/bin/env python3
"""
Simpler Plastic to Git migration using cm switch.
"""

import xml.etree.ElementTree as ET
import subprocess
import os
from collections import defaultdict

PLASTIC_HISTORY_FILE = "PLASTIC_SCM_HISTORY.xml"
GIT_AUTHOR_EMAIL = "danilodevs@gmail.com"
GIT_AUTHOR_NAME = "Danilo Devs"
WORK_DIR = "E:\\Projects\\Unity\\ClosureBT"

def run_cmd(cmd, capture=False):
    """Run command."""
    try:
        if capture:
            result = subprocess.run(cmd, shell=True, capture_output=True, text=True, cwd=WORK_DIR, timeout=30)
            return result.stdout.strip(), result.returncode
        else:
            result = subprocess.run(cmd, shell=True, cwd=WORK_DIR, timeout=30)
            return None, result.returncode
    except Exception as e:
        print(f"Error: {e}")
        return None, 1

def parse_history():
    """Parse XML history."""
    tree = ET.parse(os.path.join(WORK_DIR, PLASTIC_HISTORY_FILE))
    changesets = []
    for cs in tree.getroot().findall('Changeset'):
        cs_id = int(cs.find('ChangesetId').text)
        branch = cs.find('Branch').text
        comment = (cs.find('Comment').text or "").strip()
        date_str = cs.find('Date').text

        change_types = defaultdict(int)
        for item in (cs.find('Changes') or []):
            change_types[item.find('Type').text] += 1

        if not comment:
            parts = []
            if change_types['Added']: parts.append(f"Add {change_types['Added']} file(s)")
            if change_types['Changed']: parts.append(f"Update {change_types['Changed']} file(s)")
            if change_types['Deleted']: parts.append(f"Delete {change_types['Deleted']} file(s)")
            if change_types['Moved']: parts.append(f"Move {change_types['Moved']} file(s)")
            comment = ", ".join(parts) if parts else f"Update (changeset {cs_id})"

        changesets.append({
            'id': cs_id,
            'branch': branch,
            'comment': comment,
            'date': date_str
        })

    return sorted(changesets, key=lambda x: x['id'])

def migrate():
    """Migrate all changesets."""
    print("Parsing history...")
    changesets = parse_history()
    print(f"Found {len(changesets)} changesets\n")

    git_branches = set()
    current_branch = None

    print("Migrating...\n")
    success_count = 0

    for i, cs in enumerate(changesets):
        print(f"[{i+1:3d}/{len(changesets)}] cs:{cs['id']:3d} | ", end='', flush=True)

        # Switch workspace to this changeset
        cmd = f'cm switch cs:{cs["id"]}'
        _, code = run_cmd(cmd)
        if code != 0:
            print("X switch failed")
            continue

        # Determine Git branch
        git_branch = cs['branch'].lstrip('/').replace('/', '-')

        # Create/switch Git branch
        if git_branch != current_branch:
            check_code = run_cmd(f'git rev-parse --verify {git_branch} 2>nul')[1]
            if check_code != 0:
                run_cmd(f'git checkout -b {git_branch}')
            else:
                run_cmd(f'git checkout {git_branch}')
            current_branch = git_branch
            git_branches.add(git_branch)

        # Stage all changes
        run_cmd('git add -A')

        # Create commit with timestamp
        message = cs['comment'].replace('"', '\\"').replace("'", "'\\''")
        commit_cmd = f'git -c user.email="{GIT_AUTHOR_EMAIL}" -c user.name="{GIT_AUTHOR_NAME}" commit --allow-empty --date="{cs["date"]}" -m "{message}"'
        _, code = run_cmd(commit_cmd)

        if code == 0:
            print("OK")
            success_count += 1
        else:
            print("FAIL")

    print(f"\n{'='*70}")
    print(f"Complete: {success_count}/{len(changesets)} commits created")
    print(f"  Branches: {', '.join(sorted(git_branches))}")
    print(f"{'='*70}\n")

if __name__ == "__main__":
    migrate()
