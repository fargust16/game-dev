# 001 Documentation Standards

Version: 1.0

Status: Canonical

Last Updated: 2026-08-04

Depends On:
000 Project Rules

Referenced By:
All documentation

---

# Purpose

Ensure every project document follows a consistent structure.

---

# Required Metadata

Every document begins with:

Title

Version

Status

Last Updated

Depends On

Referenced By

---

# Writing Style

Documentation must describe systems.

Avoid implementation details unless the document is Technical Design.

Avoid speculative wording.

Avoid temporary notes.

Use present tense.

---

# Dependencies

Every document must explicitly reference:

Documents it depends on.

Documents depending on it.

---

# Versioning

Major version

Fundamental design changes.

Minor version

New sections or expanded mechanics.

Patch version

Typos, wording improvements, formatting.

---

# Promotion Rules

Draft → Canonical

Requirements:

Reviewed.

No known contradictions.

Dependencies verified.

Glossary updated.

Open Questions updated.

---

# Deprecated Documents

Deprecated documents remain inside the repository.

Never delete design history.

---

# Naming Convention

Use numeric prefixes.

Examples:

00 Vision

03 World

05 Technical Design

90 Glossary

99 Open Questions

---

# Cross References

Whenever another document is mentioned,
link it by its official name.

Never use unofficial titles.

---

# Self Containment

A reader must understand the purpose of a document without reading the chat history.
