# Middleware Backup System

This repository contains the source code for a middleware backup system that handles both local and cloud backups. 

## Overview

The system is designed to back up SQLite databases:

- **Local Backup:** Creates backups of SQLite databases in a local directory organized by year, month, and day.
- **Cloud Backup (Google Drive):** Backs up the local backups to Google Drive, syncing changes and ensuring data redundancy.

## Features

- **Local Backup:**
    - Creates backups of SQLite databases in a user-defined directory.
    - Organizes backups by year, month, and day.
    - Employs encryption to protect the backup data.
    - Performs integrity checks after each backup to ensure data consistency.
- **Cloud Backup (Google Drive):**
    - Synchronizes local backups with a designated Google Drive folder.
    - Creates new folders on Google Drive for each local backup folder.
    - Uploads files from local backup folders to their corresponding Google Drive folders.
    - Manages folder structure on Google Drive to reflect the local backup organization.

## Dependencies

The following dependencies are required:

- Google.Apis.Auth.OAuth2
- Google.Apis.Auth.OAuth2.Flows
- Google.Apis.Drive.v3
- Google.Apis.Services
- Google.Apis.Util.Store
- Newtonsoft.Json
- Microsoft.Data.Sqlite

## Configuration

1. **Google Drive Authentication:**
   - Create a service account in Google Cloud Platform.
   - Download the service account key file (JSON).
   - Place the key file in the "Resources" folder of the project.
   - Rename the key file to "credentialGoogleDrive.json".
   - Update the `parentDirectoryId` variable in `C_BackupNuvem.cs` with the ID of the target folder on Google Drive.
2. **Database Password:**
   - Define the database password in `C_BD_Globais.cs` (currently named `DB_PASSOWRD`).

## Usage

1. Compile the code.
2. Run the application.
3. The application will automatically perform local and cloud backups as configured.

## Future Improvements

- **Incremental Backups:** Implement incremental backups to reduce backup times and storage consumption.
- **Backup Scheduling:** Introduce scheduled backups to run automatically at specific intervals.
- **Error Handling:** Enhance error handling and logging to improve system resilience.
- **User Interface:** Create a user interface to manage backup configurations and monitor progress.
- **Additional Cloud Providers:** Add support for other cloud providers like AWS S3 or Azure Blob Storage.

## License

This project is licensed under the MIT License.

## Contribution

Contributions are welcome! If you find any bugs, have suggestions for improvements, or want to add new features, please feel free to submit a pull request.
