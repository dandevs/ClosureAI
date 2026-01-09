# Unity Asset Store Submission Guide

Complete guide for successfully submitting to the Unity Asset Store as of 2025.

## Table of Contents
1. [Publisher Requirements](#publisher-requirements)
2. [Technical Requirements](#technical-requirements)
3. [Package Structure & Organization](#package-structure--organization)
4. [Documentation Requirements](#documentation-requirements)
5. [Marketing & Artwork Requirements](#marketing--artwork-requirements)
6. [Asset-Specific Requirements](#asset-specific-requirements)
7. [Legal & Content Requirements](#legal--content-requirements)
8. [Submission Process](#submission-process)
9. [Common Rejection Reasons](#common-rejection-reasons)
10. [Real-World Rejection Examples from Forums](#real-world-rejection-examples-from-forums)
11. [Validation Tools](#validation-tools)
12. [Success Tips](#success-tips)
13. [Progress Checklist](#progress-checklist)

---

## Publisher Requirements

### Account Setup
- [ ] **Active email address** required
- [ ] **Actively maintained website** showcasing relevant work and skills
- [ ] Complete **publisher profile** in the Publisher Portal
- [ ] No limit on number of assets you can publish

### Getting Started
1. Create publisher account at [Asset Store Publisher Portal](https://assetstore.unity.com/publishing/)
2. Set up publisher profile
3. Submit first asset for approval

---

## Technical Requirements

### Unity Version Compatibility
- **New assets**: Must use **Unity 2021.3 or newer** (updated from 2019.4)
- **Asset updates**: Must start from **Unity 2020.3 or newer**
- If incompatible with specific versions, clearly state reasons in description

### Package Size & Structure
- **Maximum package size**: 6GB
- **File paths**: Must be under 140 characters
- **No duplicate, unusable, or redundant files**
- Must not contain files in "AssetStoreTools" folder (auto-removed)

### Package Organization
Assets must be sorted by:
- **Type**: All meshes in "Mesh" folder, materials in "Materials" folder, etc.
- **OR by relationship**: "Creature" folder containing meshes, materials, textures for that character

### Root Folder Structure
- **Single root folder** containing all assets
- Exceptions: "Gizmos", "Editor Default Resources"

### Code Quality Standards
- [ ] **No errors or warnings** in Console after setup (except handled exceptions or Unity engine bugs)
- [ ] **All code in user-declared namespaces** (not Unity or trademarked namespaces)
- [ ] **Consistent coding style** (ClassName, FunctionName, variableName, CONSTANT)
- [ ] **Readable, modifiable code** (obfuscation only for non-modifiable code)
- [ ] **Support for Android 64-bit** if applicable
- [ ] **No unsupported programming languages**
- [ ] **Proper spell-checking** of namespaces, classes, functions

### Dependencies
- Only include **necessary packages** for asset to function
- List all UPM dependencies in description and documentation
- **No GPL, LGPL, or attribution-required licenses** (including Creative Commons/Apache 2.0 requiring attribution)

---

## Package Structure & Organization

### Allowed File Types
- **Meshes**: .fbx, .dae, .abc, .obj
- **Images**: .png, .tga, .psb, .psd (lossless compression)
- **Documentation**: .txt, .md, .pdf, .html, .rtf
- **Compressed files**: .zip for non-native formats (Blender, HTML docs, Visual Studio projects) with "source" in filename

### File Naming
- **Clear, descriptive file names**
- **Not excessively long filenames**
- **Consistent naming conventions**

### Prohibited Content
- **No .unitypackage or archive files** that obscure majority of content
- **No executables** (.exe, .apk) embedded in package or as separate dependencies
- **No DRM, time restrictions, registration requirements, or extra payment gates**
- **No watermarks** obstructing product use
- **No non-secure content**: proxy servers, sensitive information handling, unsafe code

---

## Documentation Requirements

### When Documentation is Required
- Packages including **code or shaders**
- Assets with **configuration options**
- Packages requiring **setup**

### Documentation Standards
- **Required**: User guide in root folder
- **Accepted formats**: .txt, .md, .pdf, .html, or .rtf
- **Content requirements**:
  - Comprehensive setup instructions
  - Usage examples and tutorials
  - Link to online documentation (if local docs aren't comprehensive)
  - **Third-Party Notices.txt** for all fonts, audio, components with dependent licenses
  - Clear disclosure of errors/warnings with workarounds

### Video Documentation
- Must be **hosted externally** (YouTube, Vimeo, etc.)
- **NOT included** in package download
- **Recommended** for: tools, technical assets, animations

---

## Marketing & Artwork Requirements

### Package Metadata

**Title** (required):
- Cannot begin with "Unity"
  - ❌ "Unity Sculpting Tools"
  - ✅ "Sculpting Tools for Unity"

**Description** must include:
- Key features and functionality
- Dependencies and requirements
- For art assets: polygon count, texture size, render pipeline compatibility
- For scripts/tools: dependencies, third-party software requirements
- **Transparent disclosure of AI-generated content** with specific tools and modifications
- Comparison of features for multiple editions (lite/pro)
- No purely AI-generated descriptions

**Keywords**: Up to 255 characters, whitespace-separated

**Spelling/Grammar**: Must be professional, readable

### Key Images (Required)

| Image Type | Size | Purpose | Text Allowed |
|------------|------|---------|--------------|
| **Icon** | 160×160 | Icon grid view | None (no text) |
| **Card** | 420×280 | Main thumbnail | Title, publisher logo/name |
| **Cover** | 1950×1300 | Main product page | Title, publisher logo/tagline |
| **Social Media** | 1200×630 | Social media posts | None (no text) |

**Key Image Rules**:
- No Unity logos, sale banners, default Unity skybox
- No blurry, stretched, or cropped images
- Not just Editor screenshots
- Different designs for each size
- Objects/visuals must be legible at each size

### Screenshots
- Showcase assets in action
- **Multiple angles** for 3D models (textured + wireframe)
- **Demonstrate functionality** for tools
- Curators prefer screenshots for most asset types

### Audio/Video
- **Audio packages**: Must include preview in artwork section
- **Animation packages**: Must include video demonstration
- **Tools/scripting**: Video tutorial explaining setup and overview
- Accepted: YouTube, Vimeo, SoundCloud, MixCloud, SketchFab

---

## Asset-Specific Requirements

### 3D Models & Art Assets

#### Mesh Requirements
- **File types**: .fbx, .dae, .abc, .obj
- **All meshes** must have paired textures/materials and prefabs
- **Scale**: 1 unit = 1 meter
- **Pivot**: Bottom center, logical corner for modular objects, or animation pivot
- **Prefabs**: Position/rotation at (0,0,0), scale at 1
- **Forward direction**: Positive Z axis
- **Static models**: Must have colliders assigned
- **LOD models**: Must have LOD Group component with all LOD meshes
- **Photoscanned/AI data**: Must be retopologized and optimized for editing
- **Mesh density**: Reasonable topology allowing deformation

#### Animation Requirements
- **Character models**: Weighted to accompanying rig
- **Rigs**: Unity Mecanim or custom animations
- **Humanoid characters**: Mapped to Unity's default "Humanoid" rig (or disclose custom rig)
- **Animation clips**:
  - Must be sliced (no single long clips)
  - Unique names for each clip
  - Fluid movement without jarring transitions
- **Mocap data**: Must be processed and sliced
- **Must include video demonstration**

#### Texture & Material Requirements
- **Formats**: .png, .tga, .psb, .psd (lossless)
- **PBR packages**: Must include at least one of: albedo, normal, metallic/smoothness, roughness maps
- **Tileable textures**: Must tile seamlessly
- **Normal maps**: Marked as "Normal Map" in import settings
- **Texture dimensions**: Power of 2 when appropriate
- **Materials**: Properly set up with all textures
- **Alpha channel maps**: Paired with supporting shader
- **GUI elements**: Separated and named (via Sprite Editor if needed)

### Audio Assets
- **Format**: Lossless (.wav, .aiff, .flac)
- **No .mp3 or .ogg** unless with lossless files or for demo in Tools category
- **Quality**: Clear, audible, peaks below -0.3 dB
- **SFX files**:
  - No background noise
  - No excessive silence at start/end
  - Sliced into units
- **Stems**: Allowed if original full piece included or clearly disclosed
- **Must include preview** in artwork section

### Templates & Essentials
- **Designed as instructional/tutorial/framework products**
- **Must include demo scene** with visual content or functionality
- **In-depth documentation** on project design and editing/expanding
- **VR templates**: Must support 6-DoF (Full VR)

### Scripts & Tools
- **Editor windows**:
  - Menus under "Window/<PackageName>" or "Tools"
  - Purpose: functional, technical support, documentation access
  - **No marketing-only windows**
- **No automatic redirects** outside Unity Editor without user consent
- **InitializeOnLoad**: Must serve functional purpose (setup, settings, tutorials)
- **Server-based plugins**: Auto-populate databases with necessary tables

---

## Legal & Content Requirements

### Third-Party Content
- **Third-Party Notices.txt** required listing all fonts, audio, components with dependent licenses
- **License compatibility**: Must be compatible with Asset Store EULA
- **Product description must include**: "Asset uses [name of component] under [name of license]; see Third-Party Notices.txt file in package for details."
- **Prohibited licenses**: GPL, LGPL, Creative Commons/Apache 2.0 requiring attribution

### Content Restrictions
- **No genitalia** in models or images
- **No plagiarized content** or resemblance to third-party/copyrighted work
- **AI-generated content**:
  - Must have significant value and usability
  - Must disclose AI tools and content generated
  - No anatomical errors reducing usability
  - Must describe modifications adding value beyond generation

### Ownership & Rights
- **Checkbox confirmation**: Must confirm you own rights to sell assets
- **No DRM or usage restrictions**
- **No lite versions** with limited functionality unless features identical to full product

### API & Services
- **Third-party API keys**: Must be clearly described and not in builds
- **API usage terms and costs**: Must be at top of description and in documentation
- **Online services** (monetization, analytics, Web3): Consider [Verified Solutions program](https://unity.com/verified-solutions)

---

## Submission Process

### Step-by-Step

1. **Create Package Draft**
   - Access Publisher Portal
   - Create new package draft
   - Fill out all required sections

2. **Package Details**
   - **Version**: Start at 1.0 (1.3→1.4 for bug fixes, 1.3→2.0 for major changes)
   - **Version Changes**: Describe updates
   - **Category**: Choose best fit
   - **Price**: Minimum $4.99 USD (or enable Free)

3. **Upload Assets**
   - Use **Asset Store Tools** from Unity
   - Launch Unity from Publisher Portal
   - Create .unitypackage of content
   - **Validate package** (ensures no errors)

4. **Validation & Review**
   - Asset Store team reviews against Submission Guidelines
   - **Review time**: At least 5 business days
   - If no response after 2 weeks, contact Unity support

5. **Approval Process**
   - **Statuses**: Draft → Submitted → Pending → Published/Declined
   - **Auto-publish**: Checkbox to publish immediately upon approval
   - **Rejection**: Review reasons in portal or email
   - **Resubmission**: Fix issues before resubmitting
   - **Multiple resubmissions** without modification may result in account termination

6. **Preview Before Submit**
   - Use **"Preview in Asset Store"** button
   - Check all metadata, artwork, documentation
   - Verify package size and dependencies

---

## Common Rejection Reasons

1. **Errors/warnings** in Console after import
2. **Missing documentation** or insufficient setup instructions
3. **Poor organization** (files scattered, no folder structure)
4. **Missing demo scenes** (for 3D, 2D, VFX, Animation, Templates)
5. **Incomplete metadata** (description doesn't list specs, dependencies)
6. **AI-generated content** not properly disclosed
7. **Third-party licenses** incompatible with Asset Store EULA
8. **Package exceeds 6GB**
9. **Insecure code** (hardcoded API keys, sensitive data handling)
10. **DRM or usage restrictions**
11. **Code quality not up to professional standards**
12. **Poor quality marketing images**
13. **"Package is too simple"** - lack of configurability or value
14. **"Not a good fit for the store"**
15. **Outdated game mechanics**

---

## Real-World Rejection Examples from Forums

### "Package is too simple" (Most Common)

Unity's feedback:
> *"This package is too simple for our current standards. Please spend more time researching the current game market, polishing and tweaking your package to a high degree of quality, and expanding its contents to reach a broader audience."*

**Examples rejected:**
- A folder highlighting tool was rejected despite being functional
- A tile-matching game template (with 60 levels) was rejected
- A clean architecture framework without an example game

**What reviewers want:**
- More configurability (not just asset-flip ready)
- Multiple game types in bundles (3-10 different games)
- "Exciting" features that are challenging to create
- "Chore" features that save time
- **Example game** that puts code in context

### "Not up to professional standards"

Unity's feedback:
> *"The reason your package is being rejected is because it is not up to the standard of professional productions in the same gameplay genre."*

**Issues cited:**
- Poor visual design
- Low-quality audio/sounds
- Lack of variety in content
- Simple mechanics that don't meet current market standards
- Graphics style inconsistency ("random mix" of retro vs. modern)

### "Not a good fit for the store"

Generic rejection:
> *"We don't feel this package is a good fit for the store. We have chosen not to publish this asset."*

**Translation from community:**
- Asset doesn't belong in the Asset Store ecosystem
- May be obvious to those familiar with submission guidelines
- Could be too niche or doesn't align with marketplace needs

### Poor Marketing Materials

Community feedback:
- Videos with no explanation of what the asset does
- Endless scrolling code with moving mouse pointer
- No clear titles showing benefits
- No clear value proposition in first 10 seconds
- Reviewers can't understand quickly enough → decline

**One dev's experience:**
> *"Few seconds into the video I started frame stepping, then quicker and quicker because it was boring and realized in the end there's no content in it."*

### Lack of Configurability

A simple tile-matching game was rejected because:
- It can only make **one type** of game
- Only suitable for simple asset flips (replacing art/audio)
- Not configurable for gameplay/level design

### Outdated Game Mechanics

Community feedback:
> *"When was the last time games like these were popular on mobile? That may have been, at latest, ten years ago. These days players want something a lot more involved."*

### Desktop-Only Templates in Mobile Era

A project template was questioned because:
- Settings screen with selectable resolutions = desktop-focused
- Mobile/web/console/XR users wouldn't benefit
- **Solution**: Clearly indicate target platform in marketing

---

## Validation Tools

Unity provides the **Asset Store Validation Suite** (`com.unity.asset-store-validation`) to help validate packages before submission.

### Using the Validator
1. Open Unity Editor
2. Navigate to **Window > Tools > Asset Store > Validator**
3. Select **UPM** or **.unitypackage** as Validation Type
4. Run the Validator
5. Fix any reported issues and re-run validation

---

## Success Tips

### Before Submission
1. **Test thoroughly**: Import your package into a fresh project and verify everything works
2. **Provide excellent documentation**: Users abandon assets with poor documentation
3. **Show, don't just tell**: Use videos and screenshots to demonstrate functionality
4. **Be transparent**: Disclose limitations, dependencies, and AI content clearly
5. **Follow conventions**: Use Unity's naming and organization standards

### For Templates & Frameworks
1. **Include example games** that put code in context
2. **Make assets configurable** not just asset-flip ready
3. **Target underserved niches** (e.g., 4X strategy)
4. **Create bundles** with multiple related assets

### For Tools & Scripts
1. **Focus on value** - what saves time or is difficult to create?
2. **Clear marketing videos** explaining benefits in first 10 seconds
3. **Proper pricing** - $15 minimum for templates (or perceived as "total crap")
4. **Ask yourself**: Would YOU buy this for $15?

### General Best Practices
1. **Research current market thoroughly** - compare with top competitors
2. **Support your product**: Provide contact information and respond to reviews
3. **Update regularly**: Fix bugs and add features based on user feedback
4. **Price appropriately**: Research similar assets and price competitively

---

## Progress Checklist

Use this checklist to track your submission progress.

### Phase 1: Preparation
- [ ] Research similar assets on the Asset Store
- [ ] Define target audience and use cases
- [ ] Plan package structure and organization
- [ ] Identify all dependencies
- [ ] Verify all third-party licenses are compatible with EULA

### Phase 2: Technical Requirements
- [ ] Package created in Unity 2021.3 or newer
- [ ] Package size under 6GB
- [ ] All file paths under 140 characters
- [ ] Single root folder structure
- [ ] No duplicate or redundant files
- [ ] No files in "AssetStoreTools" folder

### Phase 3: Code Quality
- [ ] No errors or warnings in Console after import
- [ ] All code in user-declared namespaces
- [ ] Consistent coding style throughout
- [ ] Code is readable and modifiable
- [ ] Spell-checked all namespaces, classes, functions
- [ ] No hardcoded sensitive information (API keys, etc.)
- [ ] No DRM or usage restrictions

### Phase 4: Asset Organization
- [ ] Assets sorted by type or relationship
- [ ] Clear, descriptive file names
- [ ] Proper folder structure
- [ ] Prefabs have position/rotation at (0,0,0), scale at 1
- [ ] Meshes at 1 unit = 1 meter scale
- [ ] Proper pivot points on all models

### Phase 5: Documentation
- [ ] User guide in root folder (.txt, .md, .pdf, .html, or .rtf)
- [ ] Comprehensive setup instructions
- [ ] Usage examples and tutorials
- [ ] Third-Party Notices.txt (if applicable)
- [ ] Clear disclosure of any errors/warnings
- [ ] Link to online documentation (if local docs aren't comprehensive)
- [ ] Video tutorials created and hosted externally (if applicable)

### Phase 6: Marketing Materials
- [ ] Title created (doesn't begin with "Unity")
- [ ] Description written with all required information:
  - [ ] Key features and functionality
  - [ ] Dependencies and requirements
  - [ ] Technical details (poly count, texture size, etc.)
  - [ ] AI content disclosure (if applicable)
  - [ ] Edition comparison (lite/pro) (if applicable)
- [ ] Keywords defined (up to 255 characters)
- [ ] Icon image (160×160)
- [ ] Card image (420×280)
- [ ] Cover image (1950×1300)
- [ ] Social Media image (1200×630)
- [ ] Screenshots created (showcasing asset in action)
- [ ] Video preview created (for animations/tools)
- [ ] Audio preview included (for audio packages)

### Phase 7: Validation
- [ ] Asset Store Validation Suite run
- [ ] All validation issues fixed
- [ ] Package imported into fresh project for testing
- [ ] All functionality verified
- [ ] Demo scenes tested (if applicable)

### Phase 8: Publisher Portal Setup
- [ ] Publisher account created
- [ ] Publisher profile completed
- [ ] Website active and maintained
- [ ] Contact email verified

### Phase 9: Package Draft Creation
- [ ] Package draft created in Publisher Portal
- [ ] Version set to 1.0
- [ ] Category selected
- [ ] Price set (minimum $4.99 or Free)
- [ ] Metadata entered (title, description, keywords)
- [ ] All key images uploaded
- [ ] Screenshots uploaded
- [ ] Audio/video links added (if applicable)

### Phase 10: Package Upload
- [ ] Asset Store Tools launched from Unity
- [ ] .unitypackage created
- [ ] Package validated successfully
- [ ] Package uploaded to draft
- [ ] Ownership rights confirmed
- [ ] Auto-publish checkbox set (if desired)

### Phase 11: Pre-Submit Review
- [ ] "Preview in Asset Store" used
- [ ] All metadata verified
- [ ] All artwork checked
- [ ] Package size confirmed
- [ ] Dependencies documented
- [ ] Description proofread

### Phase 12: Submission & Follow-up
- [ ] Package submitted for approval
- [ ] Submission date recorded
- [ ] 5 business days waited
- [ ] Check Publisher Portal for status updates
- [ ] If rejected: review feedback and fix issues
- [ ] If accepted: celebrate! 🎉

### Phase 13: Post-Submission (After Approval)
- [ ] Monitor customer reviews
- [ ] Respond to customer questions
- [ ] Fix reported bugs
- [ ] Plan updates and improvements
- [ ] Update documentation based on user feedback

---

## Additional Resources

### Official Documentation
- [Asset Store Submission Guidelines](https://assetstore.unity.com/publishing/submission-guidelines)
- [Unity Manual: Submit an Asset Package](https://docs.unity3d.com/6000.5/Documentation/Manual/AssetStoreSubmit.html)
- [Unity Manual: Filling in Package Details](https://docs.unity3d.com/6000.1/Documentation/Manual/AssetStorePkgDetails.html)
- [Asset Store Publishing Workflow](https://docs.unity.cn/Manual/asset-store-workflow.html)
- [Start Publishing on Asset Store](https://assetstore.unity.com/publishing/publish-and-sell-assets)

### Validation & Tools
- [Asset Store Validation Suite](https://docs.unity3d.com/Packages/com.unity.asset-store-validation@0.3/manual/index.html)
- [Validate and upload a UPM package](https://docs.unity.cn/Manual/asset-store-upm-validate.html)

### Support & Community
- [Why was my asset declined?](https://support.unity.com/hc/en-us/articles/205754275-Why-was-my-asset-declined)
- [Asset Store Content Policy](https://unity.com/legal/asset-store-content-transparency)
- [Unity Discussions - Community Showcases](https://discussions.unity.com/c/community-showcases)

### External Resources
- [A Comprehensive Guide to Submitting to the Asset Store](https://www.gamedeveloper.com/business/a-comprehensive-guide-to-submitting-to-the-asset-store)
- [Is your asset too simple? - CodeSmile Blog](https://codesmile.de/2023/02/08/is-your-asset-too-simple/)

---

## Quick Reference

### Minimum Requirements Summary
- Unity 2021.3+ for new assets
- Package under 6GB
- No Console errors/warnings
- User guide included
- All 4 key images (Icon, Card, Cover, Social Media)
- Price $4.99 minimum or Free
- All code in user namespaces
- No GPL/LGPL/attribution licenses

### Most Common Rejection Reasons
1. "Package is too simple"
2. Poor marketing materials
3. Missing documentation
4. No demo scenes
5. Code quality issues
6. "Not a good fit for the store"

### Key Success Factors
1. **Value**: Solves a difficult problem or saves significant time
2. **Quality**: Professional-grade code and assets
3. **Documentation**: Clear, comprehensive setup and usage instructions
4. **Presentation**: High-quality marketing materials that clearly communicate benefits
5. **Completeness**: Includes examples, demos, and everything needed to use the asset

---

*Last updated: 2025*
*Document version: 1.0*
