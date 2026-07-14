# \# KolHaNitzachon Phone System Architecture

# 

# \## Overview

# 

# This project follows the principles of Clean Architecture to separate business logic, application logic, infrastructure integrations, and API endpoints.

# 

# ```

# &#x20;               +----------------------+

# &#x20;               |      Web / API       |

# &#x20;               |        API           |

# &#x20;               +----------+-----------+

# &#x20;                          |

# &#x20;                          v

# &#x20;               +----------------------+

# &#x20;               |    Application       |

# &#x20;               | Interfaces / DTOs    |

# &#x20;               +----------+-----------+

# &#x20;                          |

# &#x20;                          v

# &#x20;               +----------------------+

# &#x20;               |       Domain         |

# &#x20;               | Business Models      |

# &#x20;               | Entities             |

# &#x20;               +----------+-----------+

# &#x20;                          ^

# &#x20;                          |

# &#x20;               +----------------------+

# &#x20;               |   Infrastructure     |

# &#x20;               | Azure                |

# &#x20;               | SignalWire           |

# &#x20;               | Payment Gateway      |

# &#x20;               | Repositories         |

# &#x20;               +----------------------+

# ```

# 

# \---

# 

# \## Project Structure

# 

# \### API

# 

# Responsible for:

# 

# \- Controllers

# \- Dependency Injection

# \- Authentication

# \- Middleware

# \- Configuration

# 

# \---

# 

# \### Application

# 

# Responsible for:

# 

# \- Interfaces

# \- DTOs

# \- Application contracts

# \- Use cases

# 

# Application never knows how external services are implemented.

# 

# \---

# 

# \### Domain

# 

# Responsible for:

# 

# \- Entities

# \- Value Objects

# \- Enums

# \- Business Rules

# 

# The Domain has no dependency on Infrastructure.

# 

# \---

# 

# \### Infrastructure

# 

# Responsible for implementing:

# 

# \- Payment Gateway

# \- Azure Blob Storage

# \- SignalWire

# \- Logging

# \- Database

# \- External APIs

# 

# \---

# 

# \## Dependency Flow

# 

# ```

# API

# &#x20;↓

# Application

# &#x20;↑

# Infrastructure

# 

# Domain

# ```

# 

# The API depends only on abstractions.

# Infrastructure implements those abstractions.

