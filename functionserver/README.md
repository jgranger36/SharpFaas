# sharpfaas 
``` 
.net plugin based api. functions following a specific format can be pushed to the server and launched by calling a singalr or 
standard web api endpoint. The passed in function name and version will be loaded into a unique load context and the functions
ExecuteAsync() method will be launched passing in the callers json payload. Regular old executables can be ran as well and the payload
will be passed in as an argument.
```

## Function Server 
- this is simple api that implements a few simple endpoints
- Endpoints:
  - 'api/pushfunction'
    - POST
    - parameters
      - functionName - name of the function, must be unique across all functions in store
      - type - the type of function architecture, either exe or dll
      - lane - this is the lane to push the function into for example: dev, production etc. 
      - newVersion - bool - if this is true a new major version will be generated, other wise a minor version is generated
      - file - must be a zip file of your .net 6 class library including the implemented IFunction library
    - example
      - curl --location --request POST 'http://sharpfaas_server/api/function/pushfunction?functionName=ExampleFunction&type=dll&lane=dev&newVersion=true' \
        --form 'file=@"/path/to/file"'
  - 'api/runfunction'
    - POST 
    - parameters 
      - callerId - string - unique id of a specific caller. primarily for log parsing
      - functionName - name of function to schedule
      - lane - name of lane to get function from
      - version - version of function to run in lane. this is a standard 2 character semantic version format, example: 1.1
      - payload - json payload to pass down to the function
      - payloadEncrypted - bool - if payload was encrypted with the shared key
      - type - type of the function, exe or dll
      - entryFileName - - 
  - 'functions'
    - SignalR POST
    - parameters
      - functionName - name of function to launch 
      - lane - lane that function should be pulled from
      - version - version of function to use from lane
      - payload - json payload to be passed down to function on launch
    - returns a stream of strings which are the console output from the launched function

## Configuration 
- you must configure an appsettings.json file with the following data 
  - local folder that will store functions for running
  - ftp server that functions will be centrally pushed to and pulled from
  - a sql server database that will house the hangfire schedule database 
  - port to use for the api server

## FunctionStore 
- this is current just an ftp folder that all functionservers have access to. Any functions pushed to any server will 
be pushed to this ftp and the version will be incremented automatically
- Functions are stored in lanes that allow creating multiple version paths for a function. For example, you may have dev lane
that allows pushing up new and untested version of a function for runnign from a dev environment. At the same time, you may have
a production lane that always housing the most recent production ready version of a function.
- Every push to a lane increments the minor version number
- lanes and versions are simply folder paths: 'server/functionstore/lane/version/function'
- if newVersion boolean parameter is passed in as true, a new major version is created, this can be used to designate a major or breaking change to the code

### Function Requirements 
- must use .net 6 
- must include the SharFaas.Core nuget library
- must have a class named FunctionHandler that implements the IFunction interface from the sharpfaas_interfaces library
- the FunctionHanlder class must impliment the interfaces ExecuteAsync(string payLoad) method, accepting a string parameter
that will be the passed in jsonPayload
- it is recommended to keep referenced libraries to a minimum, though there is no recommended threshold. This is strictly to 
ensure better support for unloading the function after running as some libraries do not allow unloading.

## Executables
- should be of a console type and not load any ui components as there will be no interactivity capable. 
- should wrap itself in a try catch to ensure application always closes gracefully 
- should set some form of time out to stop things from running for days. 