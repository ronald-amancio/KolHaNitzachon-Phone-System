# \# Folder Structure

# 

# ```

# KolHaNitzachon.PhoneSystem

# 

# API

# │

# ├── Controllers

# ├── Extensions

# ├── Middleware

# ├── Configurations

# 

# Application

# │

# ├── Interfaces

# │   ├── Email

# │   ├── External

# │   ├── Identity

# │   ├── Notifications

# │   ├── Payment

# │   ├── Sms

# │   ├── Storage

# │   └── Voice

# 

# Domain

# │

# ├── Constants

# ├── Entities

# ├── Enums

# ├── Services

# └── ValueObjects

# 

# Infrastructure

# │

# ├── Azure

# ├── External

# ├── Identity

# ├── Logging

# ├── Payment

# ├── Persistence

# ├── Repositories

# ├── SignalWire

# └── Twilio

# ```

# 

# \## Naming Convention

# 

# Interfaces

# 

# ```

# IEmailService

# 

# IBlobStorageService

# 

# IPaymentGatewayService

# ```

# 

# Implementations

# 

# ```

# EmailService

# 

# BlobStorageService

# 

# SolaPaymentGatewayService

# ```

# 

# Controllers

# 

# ```

# CustomerController

# 

# PaymentController

# 

# VoiceController

# ```

# 

# Namespaces follow the project structure.

