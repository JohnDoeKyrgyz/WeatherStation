#I __SOURCE_DIRECTORY__
#r @"packages\Newtonsoft.Json\lib\net45\Newtonsoft.Json.dll"

#if INTERACTIVE
#r @"packages\FSharp.Data\lib\net45\FSharp.Data.dll"
#I @"C:\Users\jatwood\AppData\Roaming\npm\node_modules\azure-functions-core-tools\bin"
#r "Microsoft.Azure.Webjobs.Host.dll"
#r "System.Net.Http.dll"
#r "System.Net.Http.Formatting.dll"
#r "Microsoft.WindowsAzure.Storage.dll"
#else
#r "FSharp.Data"
#endif

#r "System.Net.Http"
#r "Microsoft.Azure.WebJobs"
#r "Microsoft.WindowsAzure.Storage"
