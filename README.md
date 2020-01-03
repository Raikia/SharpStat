# SharpStat

C# utility that uses WMI to run "cmd.exe /c netstat -n", save the output to a file, then use SMB to read and delete the file remotely

## Description

This script will attempt to connect to all the supplied computers and use WMI to execute `cmd.exe /c netstat -n > <file>`. The file the output is saved to is specified by '-file'. Once the netstat command is running, the output is read via remote SMB call and then deleted.

While this isn't the stealthiest of scripts (because of the cmd.exe  execution and saving to a file), sometimes you gotta do what you gotta do. An alternative would be to use WMI to remotely query netstat information, but that WMI class is only available on Win10+ systems, which isn't ideal.  This solution at least works for all levels of operating systems.


## Usage

     Mandatory Options:
         -file         = This is the file that the output will be saved in 
                         temporarily before being remotely read/deleted

     Optional Options:
         -computers    = A list of systems to run this against, separated by commas
            [or]
         -dc           = A domain controller to get a list of domain computers from
         -domain       = The domain to get a list of domain computers from



## Examples

         SharpStat.exe -file "C:\Users\Public\test.txt" -domain lab.raikia.com -dc lab.raikia.com
         SharpStat.exe -file "C:\Users\Public\test.txt" -computers "wkstn7.lab.raikia.com,wkstn10.lab.raikia.com"

## Contact

If you have questions or issues, hit me up at raikiasec@gmail.com or @raikiasec


## License
[MIT](https://choosealicense.com/licenses/mit/)
