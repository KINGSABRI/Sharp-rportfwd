# Sharp-rportfwd
A CSharp implementation of Reverse Port Forwarder. 

### Features
- It supports a kill switch port to kill the application's process once the port is called/scanned/telneted. 
- Kill switch is optional; it will not listen to a default port if not assigned.

### Usage
```
rportfwd.exe <LPORT> <RHOST:RPORT> [KILL_SWITCH_PORT]
```

**Example:**
```
rportfwd.exe 4444 localhost:5555 9911

[*] Kill switch active on port 9911
[*] Listening on 0.0.0.0:5555 -> forwarding to localhost:4444
[!] Kill switch armed. Connect to port 9911 to terminate.
```

### Compilation
You can use "Visual Studio Developer Command Prompt" to compile it using CSC
```
csc /optimize+ /debug- /platform:x64 /out:rportfwd.exe Program.cs
```



