# \# Git Branching Strategy

# 

# This project follows a Feature Branch workflow.

# 

# \## Branches

# 

# \### master

# 

# Production-ready code.

# 

# Only reviewed and tested code should be merged into this branch.

# 

# \---

# 

# \### staging

# 

# Integration branch.

# 

# All completed features are merged here first for testing.

# 

# \---

# 

# \### feature/\*

# 

# Every new feature should have its own branch.

# 

# Examples:

# 

# ```

# feature/payment-gateway

# 

# feature/blob-storage

# 

# feature/signalwire

# 

# feature/email

# 

# feature/voice

# ```

# 

# \---

# 

# \## Workflow

# 

# ```

# feature/\*

# &#x20;     │

# &#x20;     ▼

# &#x20;staging

# &#x20;     │

# &#x20;     ▼

# &#x20;master

# ```

# 

# \---

# 

# \## Development Process

# 

# 1\. Pull latest staging.

# 

# ```

# git checkout staging

# git pull origin staging

# ```

# 

# 2\. Create feature branch.

# 

# ```

# git checkout -b feature/my-feature

# ```

# 

# 3\. Develop.

# 

# 4\. Commit changes.

# 

# 5\. Push feature branch.

# 

# 6\. Create Pull Request into staging.

# 

# 7\. Review and merge.

# 

# 8\. Delete feature branch.

