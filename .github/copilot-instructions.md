# Copilot Instructions

- Always consult the following files in the project root before generating code:
  - `.augment/system-prompt.md`
  - `.augment/rules.md`
  - `/docs/INSTRUCTIONS.md`
  - `/docs/INTEGRATION.md`

- All new features must follow the feature-based structure:
  `/src/<FeatureName>/{Domain,Application,Infrastructure}`

- Never place code directly in the project root.
- Always update `/docs/INSTRUCTIONS.md` and `/docs/INTEGRATION.md` if you generate new code that affects structure.
