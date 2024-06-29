AV-API is primarily a C# dotnet 8 application.

However, there is a manager UI that is written in X64 Delphi. The Delphi App starts/stops the AV-API webapp using a Windows GUI. The AV-Bible-Addin-For-Microsoft-Word will reference the AV-Bible-API-Manager in the help files. As AV-API is a minimal WebApp and runs as a console, the Delphi App is a little more user friendly, even if it adds complexity to the mix. Yet, all it does is start/stop the AV-API and show status of whether the AV-API is running or not. The Delphi App itself is stateless and single-instance (it can be closed without stopping the AV-API, and relaunched at will)