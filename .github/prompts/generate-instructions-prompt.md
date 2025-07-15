---
mode: 'agent'
model: Claude Sonnet 4
tools: ['githubRepo', 'codebase']
description: 'Generate instruction files for a CQRS application architecture in a People Management System.'
---
# Generate instruction files for a CQRS application architecture in a People Management System

This prompt is designed to create the necessary instruction files for a CQRS (Command Query Responsibility Segregation) application architecture that supports storing and retrieving data in conformance with the business rules of a People Management System. The system includes features for managing employees, departments, and roles.

The model is based on the ORM white paper, which provides a structured approach to object role modeling.

The generated files will include:
- Copilot instruction files for the CQRS application architecture.
- Instruction files for provisioning resources in Azure.
- Instruction files for configuration and secrets management.
- Additional instruction files needed before creating the application.
- Prompts for creating, provisioning, deploying, configuring, and testing the application. Including instructions for testing the implementation after each prompt.

The context includes a diagram of the Object Role Model (ORM) for the People Management System, which can be used to inform the design of the application.
