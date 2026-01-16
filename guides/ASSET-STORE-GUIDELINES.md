# Unity Asset Store Submission Guidelines

Complete reference for publishing successful Unity assets.

## Table of Contents
- [Submission Requirements](#submission-requirements)
- [Documentation Standards](#documentation-standards)
- [Demo Scenes](#demo-scenes)
- [Technical Requirements](#technical-requirements)
- [Marketing & Presentation](#marketing--presentation)
- [Pricing Strategy](#pricing-strategy)
- [Review Process](#review-process)
- [Post-Launch Checklist](#post-launch-checklist)

---

## Submission Requirements

### Unity Version Compatibility
| Submission Type | Minimum Unity Version |
|-----------------|----------------------|
| New assets | Unity 2021.3 LTS or newer |
| Updates to existing | Unity 2020.3 LTS or newer |

**Recommendation:** Target Unity 2021.3 LTS for maximum compatibility.

### Required Components

```
Your Asset Package/
├── Documentation/
│   ├── Manual.pdf (or .rtf)
│   └── Changelog.txt
├── Demo/
│   └── DemoScene.unity
├── Editor/
│   └── (Editor scripts in this folder)
├── Scripts/
│   └── (Runtime scripts)
├── Prefabs/
├── Materials/
└── README.txt
```

### Licensing Rules

**Prohibited Licenses:**
- ❌ GPL / LGPL
- ❌ Creative Commons with Attribution requirement
- ❌ Any license requiring open-sourcing derivative work
- ❌ Code copied from Stack Overflow without MIT relicensing

**Allowed:**
- ✅ MIT License
- ✅ Apache 2.0
- ✅ BSD
- ✅ Public Domain / CC0
- ✅ Original code you own

### Content Restrictions

- ❌ No third-party trademarks or logos
- ❌ No copyrighted characters or resemblance to them
- ❌ No links to competing marketplaces
- ❌ No anatomical errors (for character assets)
- ❌ No placeholder or incomplete content
- ✅ All advertised features must be functional

---

## Documentation Standards

### Required Documentation

For **Scripts and Shaders**, documentation is MANDATORY:
- PDF or RTF format (not Markdown)
- Tutorial video demonstrating the asset

### Documentation Checklist

```markdown
## Your Asset Manual - Required Sections

1. **Introduction**
   - What the asset does
   - Key features list
   - System requirements
   - Unity version compatibility

2. **Quick Start Guide**
   - Installation steps
   - Basic setup (< 5 minutes to first result)
   - First demo scene walkthrough

3. **Complete Feature Guide**
   - Each feature explained with:
     - Description
     - How to use
     - Code example
     - Inspector screenshot

4. **API Reference**
   - All public classes
   - All public methods with parameters
   - Return values and exceptions
   - Code snippets for common use cases

5. **Troubleshooting**
   - Common issues and solutions
   - Error messages explained
   - Performance optimization tips

6. **Changelog**
   - Version history
   - New features per version
   - Bug fixes
   - Breaking changes

7. **Support Information**
   - Contact email
   - Forum/Discord link
   - Response time expectations
```

### Tutorial Video Requirements

**Minimum Requirements:**
- Show installation process
- Demonstrate core functionality
- Walk through demo scene
- Show at least one practical use case

**Recommended:**
- 5-15 minutes length
- Clear audio narration
- HD quality (1080p minimum)
- Host on YouTube (unlisted is fine)

---

## Demo Scenes

### Why Demo Scenes Matter

- First thing reviewers and customers test
- Proves your asset works
- Showcases capabilities
- Reduces refund requests

### Demo Scene Checklist

```csharp
// Include a demo controller script like this:
public class DemoController : MonoBehaviour
{
    [Header("Demo Controls - Press these keys!")]
    [SerializeField] private string[] _controlInstructions = new[]
    {
        "WASD - Move",
        "Space - Jump",
        "1-5 - Switch Demo Modes",
        "R - Reset Demo"
    };

    private void Start()
    {
        // Show instructions on start
        ShowInstructions();
    }

    private void OnGUI()
    {
        // Always visible help text
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("=== DEMO CONTROLS ===");
        foreach (var instruction in _controlInstructions)
        {
            GUILayout.Label(instruction);
        }
        GUILayout.EndArea();
    }
}
```

### Demo Scene Best Practices

1. **Self-explanatory** - User should understand without reading docs
2. **Interactive** - Let users experiment with features
3. **Showcase variety** - Demonstrate different configurations
4. **Include UI** - On-screen instructions and controls
5. **Clean hierarchy** - Organized GameObject structure
6. **Works immediately** - No setup required to press Play

### Multiple Demo Scenes

```
Demo/
├── Demo_QuickStart.unity     # Simplest possible demo
├── Demo_AllFeatures.unity    # Comprehensive showcase
├── Demo_Performance.unity    # Stress test with many agents
└── Demo_Integration.unity    # Shows integration with other systems
```

---

## Technical Requirements

### Code Quality Standards

```csharp
// GOOD: Clean, documented, follows conventions
namespace YourCompany.YourAsset
{
    /// <summary>
    /// Calculates A* pathfinding on a grid.
    /// </summary>
    public class Pathfinder : MonoBehaviour
    {
        [Tooltip("Size of each grid cell in world units")]
        [SerializeField] private float _cellSize = 1f;

        /// <summary>
        /// Finds optimal path between two points.
        /// </summary>
        /// <param name="start">Starting position</param>
        /// <param name="end">Target position</param>
        /// <returns>List of waypoints, or null if no path</returns>
        public List<Vector3> FindPath(Vector3 start, Vector3 end)
        {
            // Implementation
        }
    }
}
```

### Namespace Your Code

Always use namespaces to prevent conflicts:

```csharp
// BAD: Global namespace, will conflict with user's code
public class Pathfinder { }

// GOOD: Namespaced
namespace EasyPath
{
    public class Pathfinder { }
}

// BETTER: Company + Asset namespace
namespace YourCompany.EasyPath
{
    public class Pathfinder { }
}
```

### Assembly Definitions

Use Assembly Definitions for:
- Faster compilation
- Clear dependency management
- Platform-specific code

```
Scripts/
├── YourAsset.Runtime.asmdef      # Runtime code
Editor/
├── YourAsset.Editor.asmdef       # Editor-only code
```

### Platform Compatibility

Test on these platforms at minimum:
- ✅ Windows (Standalone)
- ✅ macOS (Standalone)
- ✅ Android (if applicable)
- ✅ iOS (if applicable)
- ✅ WebGL (if applicable)

### Error Handling

```csharp
// Provide clear error messages
public void SetTarget(Transform target)
{
    if (target == null)
    {
        Debug.LogError("[EasyPath] Target cannot be null. " +
                      "Please assign a valid Transform.");
        return;
    }
    
    _target = target;
}

// Validate in Inspector
private void OnValidate()
{
    if (_gridSize < 1)
    {
        Debug.LogWarning("[EasyPath] Grid size must be at least 1. " +
                        "Setting to minimum value.");
        _gridSize = 1;
    }
}
```

---

## Marketing & Presentation

### Key Images

| Image Type | Requirements |
|------------|-------------|
| Icon | 160x160 px |
| Card Image | 420x280 px |
| Cover Image | 1200x630 px |
| Screenshots | Min 1200px wide, max 500MB total |

### Screenshot Best Practices

1. **Show the asset in action** - Not just inspectors
2. **High resolution** - 1920x1080 minimum
3. **Clean UI** - Hide unnecessary Unity panels
4. **Varied angles** - Show different features
5. **Before/After** - If applicable
6. **Code snippets** - Show simple API usage

### Promotional Images

**Do:**
- Show actual asset functionality
- Use Unity Editor screenshots
- Include feature callouts
- Show performance metrics if impressive

**Don't:**
- Use misleading renders
- Include registered trademarks
- Promise features you don't have
- Use stock photos unrelated to asset

### Description Writing

```markdown
# Your Asset Name

**One-sentence value proposition** that tells users exactly what they get.

## ✨ Key Features
- Feature 1 - brief explanation
- Feature 2 - brief explanation
- Feature 3 - brief explanation

## 🚀 Quick Start
1. Import the package
2. Add component to GameObject
3. Press Play!

## 📋 What's Included
- Full C# source code
- Comprehensive documentation
- Demo scenes
- Tutorial video

## 💻 Requirements
- Unity 2021.3 LTS or newer
- No other dependencies

## 📧 Support
Email: support@yourcompany.com
Discord: discord.gg/your-server
Response time: 24-48 hours

## 📝 Changelog
See documentation for full changelog.
```

---

## Pricing Strategy

### Market Research

Before pricing, research competitors:

| Price Range | Asset Type |
|-------------|------------|
| $10-25 | Simple utilities, small tools |
| $25-50 | Full-featured single systems |
| $50-100 | Comprehensive toolkits |
| $100+ | Enterprise/professional tools |

### Pricing Factors

**Higher price justified by:**
- Unique functionality (no alternatives)
- Extensive features
- Excellent documentation
- Active support/updates
- Performance optimization
- Editor tools included

**Lower price appropriate for:**
- Many competitors exist
- Simple/single-purpose tool
- New publisher (building reputation)
- Limited support capacity

### Sale Strategies

- **Launch discount**: 20-30% off for first week
- **Unity sales**: Participate in seasonal sales
- **Bundle pricing**: Offer discounts for multiple assets
- **Upgrade pricing**: Discount for existing customers

---

## Review Process

### Timeline

- **Initial review**: 5+ business days
- **Re-submission after fixes**: Back to end of queue
- **Updates to existing**: Usually faster

### Common Rejection Reasons

1. **Missing documentation** - Most common for scripts
2. **Broken demo scene** - Always test before submitting
3. **Unity version too old** - Must be 2021.3+
4. **Licensing issues** - Check all third-party code
5. **Missing content** - All advertised features must work
6. **Trademark issues** - No logos in screenshots

### Pre-Submission Checklist

```markdown
## Before You Submit

### Package Contents
- [ ] All scripts compile without errors
- [ ] All prefabs have no missing references
- [ ] Demo scenes work in fresh project
- [ ] No console errors or warnings
- [ ] Documentation PDF/RTF included
- [ ] Tutorial video linked

### Technical
- [ ] Tested in Unity 2021.3 LTS
- [ ] Tested in empty project
- [ ] No third-party dependencies (or documented)
- [ ] All code in namespaces
- [ ] No compiler warnings

### Legal
- [ ] No GPL/LGPL code
- [ ] No trademarked images
- [ ] All assets are original or properly licensed
- [ ] No links to other marketplaces

### Quality
- [ ] Documentation covers all features
- [ ] Demo scene is self-explanatory
- [ ] Error messages are helpful
- [ ] Inspector tooltips added
```

---

## Post-Launch Checklist

### First Week

- [ ] Monitor reviews daily
- [ ] Respond to all questions within 24h
- [ ] Document any reported issues
- [ ] Plan first update based on feedback

### Ongoing

- [ ] Update for new Unity versions
- [ ] Add requested features
- [ ] Improve documentation based on questions
- [ ] Engage with community
- [ ] Post development updates

### Support Best Practices

```markdown
## Support Response Template

Hi [Customer Name],

Thank you for purchasing [Asset Name]!

[Answer their question directly]

Here are the relevant documentation sections:
- [Link to specific page]
- [Link to video timestamp]

If you have any other questions, please don't hesitate to ask.

Best regards,
[Your Name]
```

### Handling Negative Reviews

1. **Respond quickly** - Within 24 hours
2. **Be professional** - Never defensive
3. **Offer solution** - Fix or refund
4. **Follow up** - Ask them to update review after resolution

---

## Quick Reference: Our Assets

### EasyPath ($35)
**Target**: Beginner Unity developers
**Key selling points**:
- Simpler than A* Pathfinding Project
- One-click setup
- Visual debugging
- Full source code

**Documentation focus**: Step-by-step tutorials, beginner-friendly language

### SwarmAI ($45)
**Target**: RTS/colony sim developers
**Key selling points**:
- Built from competition experience (Battlecode)
- Multi-agent coordination out of the box
- Performance optimized for many units
- ID-based agent differentiation

**Documentation focus**: Architecture explanation, performance guides

### NPCBrain ($60)
**Target**: Intermediate developers
**Key selling points**:
- All-in-one solution
- Pathfinding + Behaviors + Sensing
- Modular architecture
- Advanced features for power users

**Documentation focus**: API reference, advanced use cases
