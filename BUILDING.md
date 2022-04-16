# Developer Notes



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
