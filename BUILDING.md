# Developer Notes

## Pre-requisites

 - Docker
 - Dotnet Core 3.1 
 
 ## Solution Files
 
 The `src` directory contains two solution files for working with the code-base. 
 
 The `DataDock.Windows.sln` includes a reference to `DataDock.ImportUI\DataDock.ImportUI.njsproj` which is a project file format that is only supported on Visual Studio for Windows with the Node.js development workload installed. 
 The `DataDock.sln` excludes this project file, meaning that to build the Import Vue app, you must use the `npm` command line (see Visual Studio Code instructions below).
    

## Dev 

- run `npm install` in `src/DataDock.ImportUI`

- Fill out local config in `/src/DataDock.Web/appsettings.development.json` (create if none exists, this file is excluded from git)

- Fill out local config in `/src/DataDock.Worker/appsettings.development.json` (create if none exists, this file is excluded from git)

If developing using Visual Studio Code, add these tasks to your `tasks.json`:

```json

	{
        "label": "sln build",
        "command": "dotnet",
        "type": "process",
        "args": [
            "build",
            "${workspaceFolder}/src/DataDock.sln"
        ],
        "problemMatcher": "$msCompile"
    },
    {
        "label": "vue build",
        "type": "npm",
        "script": "build",
        "options": {
            "cwd": "${workspaceFolder}/src/DataDock.ImportUI"
         },
    },
    {
        "label": "build all",
        "dependsOn": ["sln build", "vue build"],
        "problemMatcher": "$msCompile"
    }
```

 - run a build in `src` dir
 - run `docker-compose -f docker-compose.yml -f docker-compose.override.yml up --build --force-recreate` from `/src`

# Docker on Windows

The Docker VM needs additional configuration to support running the Elasticsearch image.
Elasticsearch fails to start with a message like: 

```
ERROR: [1] bootstrap checks failed

[1]: max virtual memory areas vm.max_map_count [65530] is too low, increase to at least [262144]
```

The solution is to set vm.max_map_count in the sysctl.conf of the VM host machine.
On Windows with WSL this can be done from Powershell:

```
wsl -d docker-desktop
```

then edit /etc/sysctl.conf to add the following line:

```
vm.max_map_count = 262144
```

then update the value for the currently running kernel:

```
sysctl -w vm.max_map_count=262144
```
