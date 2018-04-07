#I __SOURCE_DIRECTORY__
#r @"packages\Newtonsoft.Json\lib\net45\NewtonSoft.Json.dll"

#if INTERACTIVE
#I @"node_modules\azure-functions-core-tools\bin"
#r "Microsoft.Azure.Webjobs.Host.dll"
#r "System.Net.Http.dll"
#r "System.Net.Http.Formatting.dll"
#r "System.Web.Http.dll"

#endif

#r "System.Net.Http"
#r "Newtonsoft.Json"