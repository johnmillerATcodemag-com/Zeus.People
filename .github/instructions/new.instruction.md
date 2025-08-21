Treat warnings as errors in the codebase. This means that any warning generated during the build or test process should be treated as a failure, and the code should not be merged until all warnings are resolved. This helps maintain code quality and ensures that potential issues are addressed promptly.

Update readme files to reflect the current state of the application. This includes updating documentation to accurately describe the functionality, usage, and any changes made to the codebase. Clear and up-to-date documentation is essential for maintainability and for onboarding new developers.

Ensure that all code is properly formatted and adheres to the project's coding standards. This includes consistent indentation, naming conventions, and overall code style. Proper formatting improves readability and maintainability of the code.

Update architecture diagrams to reflect the current state of the application. This includes any changes to the system architecture, components, and their interactions. Accurate architecture diagrams help in understanding the system design and are useful for future development and maintenance.

Create smoke tests to verify the basic functionality of the application. Smoke tests are a subset of tests that cover the most critical paths in the application, ensuring that the main features work as expected. This helps catch major issues early in the development process. Run the smoke test after every deployment to ensure that the application is functioning correctly in the deployed environment.

Provision resources in the Azure SouthCentralUS region. This includes setting up necessary Azure resources such as virtual machines, databases, and storage accounts in the specified region to ensure optimal performance and compliance with regional requirements.

Use the Azure Configuration Management Service to manage the configuration of the application. This service allows for centralized management of application settings, secrets, and other configuration data, making it easier to maintain and update configurations across different environments.

New addition to the business-rules instructions:
* Workflows are use cases that guide data capture and processing in the application. Implement the following workflows:
    * Promote a Lecturer to Senior Lecturer
    * Promote a Senior Lecturer to Associate Professor
    * Promote an Associate Professor to Professor
    * Assign a Class to an Academic
    * Add a new Academic to the faculty capturing all required information and allowing the capture of optional information. All information captured must conform to the business rules at all times.
    * Demote a Professor to Associate Professor
    * Demote an Associate Professor to Senior Lecturer
    * Demote a Senior Lecturer to Lecturer
    * Remove an Academic from the faculty, ensuring all dependencies and business rules are satisfied
    * Transfer an Academic between departments, validating eligibility and updating all related records
    * Assign a Research Project to an Academic, ensuring project requirements and academic qualifications are met
    * Update an Academic's personal or professional information, enforcing validation and business rules
    * Approve or reject leave requests for Academics, following leave policies and business rules
    * Assign administrative roles (e.g., Head of Department) to eligible Academics, ensuring compliance with business rules
* Workflow implementations must be shown to work before merging to main

- Implement automated testing for all new features and bug fixes. This includes unit tests, integration tests, and end-to-end tests to ensure comprehensive coverage and prevent regressions.

## cqrs-architecture

### Documentation
- Overview of CQRS principles and patterns
- Explanation of command and query responsibilities
- Guidelines for implementing CQRS in the application
- Update mermaid architecture diagrams with any infrastructure, architecture, or business rule changes


expect 200 response codebase