Description:
	Library for distributing automatic updates of your software from your Google Drive.
	For example, if you want to create a launcher for your games or other programs,
	you can use this library instead of renting a server to update the software on user computers.
	(15 GB of storage is free on Google Drive).

I used the following APIs: 
	Google Drive API: https://developers.google.com/drive/
	DotNetZip: https://www.nuget.org/packages/DotNetZip/

Licence:
	Please check Licence.txt.
	
How it works:
	GDUpSynchronizer:
		Checks the files in the folder on the local PC and on Google Drive.
		Deletes those files on Drive, which are no longer needed (deleted or modified on local).
		Creates zip files with password from uploadable files, which you didn't add to exceptions. (For example, log files.)
		Uploads the zip files.
		Deletes the zip files on local.
		
	GDDownSynchronizer:
		Checks the files in the folder on the local PC and on Google Drive.
		Deletes those files on the local PC, which are no longer needed and which you didn't add to exceptions. 
			(For example, it wouldn't be good, if the synchronizer deleted the settings or backups of the user.) 
		Downloads the new files.
		Unzips the files with the password the uploader used.
		Deletes the zip files on local.
		
How to install:
1. Create a project on https://console.developers.google.com.
	Enable the Google Drive API and create the credentials.

2. Download the json file containing the certificate.
	You can use this in your programs. 
	You will need it if you want to try my demo programs.

3. Install the Google Drive API v3 and DotNetZip with package manager:
	Install-Package Google.Apis.Drive.v3
	Install-Package DotNetZip -Version 1.11.0 (I think you can use a newer version, if available.)
