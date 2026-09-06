**windows installation commands**
    filter the commands and the stepd from this = Getting started
What you first need to do is open CMD (Command Prompt) as Administrator using this keyboard key:

Windows-logo key+ R

And now type in "cmd.exe" in the box

It should now look as something like this:

image

Now press this keys on your keyboard:

ctrl+shift+enter

Now you have something like this:

image

Now, click on "yes"

Now you have something like this:

image

The commands
Now, type the following command: slmgr.vbs /upk Now it will give an message, click on OK

And now this command: slmgr.vbs /cpky It will give an message once again, and click on OK again

And now type this command: slmgr.vbs /ckms Once again click on OK when you get an message

Edition upgradable check command
Now we are gonna check of your edition is supported to upgrade to Pro, run the following command to check this: DISM /online /Get-TargetEditions If you see "Professional" in the list, then you can upgrade your Windows edition to Pro for free!

Running Windows Pro installer
Now, copy and paste this complete command:

sc config LicenseManager start= auto & net start LicenseManager

sc config wuauserv start= auto & net start wuauserv

changepk.exe /productkey VK7JG-NPHTM-C97JM-9MPGT-3V66T

exit

It will run an installer and you will see an message:"% complete"

Now wait until it's 100% and it's not weird that you will get an error

When you get the error, just click Exit and then reboot your pc.

You will now see an message that he is running updates and is installing features, just wait until its done and check "info" in settings, You will see that Windows 11 Pro is installed!

But we are not done, You will see that it isn't activated and that you can't change some settings, now we are gonna fix that!

Activating Windows Pro
Now we are gonna run some other commands to activate Windows 11 Pro

Press these keyboard keys once again:

Windows-logo key+R

It looks like this again:

Run Dialog With cmd.exe Text In It

Press ctrl+shift+enter

You will get an message, just click on Yes

Now you will get an Command Prompt.

Type the following commands one for one to activate:

slmgr /ipk W269N-WFGWX-YVC9B-4J6C9-T83GX

slmgr /skms kms8.msguides.com

slmgr /ato

Now you have Windows 11 Pro and it activated! You can check settings to see it. 


**office activation commands**
filter from this = ### Method 1 - PowerShell ❤️

1. Click the **Start Menu**, type `PowerShell`, and open it.

2. Copy and paste the code below and press **Enter.**  
   - For **Windows 8.1, 10 and 11**:
     ```
     irm https://get.activated.win | iex
     ```
	 If the above is blocked (by ISP/DNS), try this (needs updated Windows 10 or 11):  
	 ```
	 iex (curl.exe -s --doh-url https://1.1.1.1/dns-query https://get.activated.win | Out-String)
	 ```
	- **Script not launching? Use the below-listed Method 2.**

3. In the menu that appears, type the number corresponding to one of the **Green** options.

---

### Method 2 - Traditional (Windows Vista and later)

1.   Download the script:
      *   [**MAS_AIO.cmd**](https://dev.azure.com/massgrave/Microsoft-Activation-Scripts/_apis/git/repositories/Microsoft-Activation-Scripts/items?path=/MAS/All-In-One-Version-KL/MAS_AIO.cmd&download=true) (Direct script)
      *   [**MAS_AIO.zip**](https://dev.azure.com/massgrave/Microsoft-Activation-Scripts/_apis/git/repositories/Microsoft-Activation-Scripts/items?$format=zip) (If the direct script is blocked by your browser)
2.   Run the `MAS_AIO.cmd` file.
3.   In the menu that appears, type the number corresponding to one of the **Green** options.

---

> [!TIP]
> - Some ISPs/DNS providers block access to our domains. You can bypass this by enabling [DNS-over-HTTPS (DoH)](https://developers.cloudflare.com/1.1.1.1/encryption/dns-over-https/encrypted-dns-browsers/) in your browser. 
> - **Having trouble**? Visit our [troubleshooting page](https://massgrave.dev/troubleshoot) or raise an issue on [GitHub](https://github.com/massgravel/Microsoft-Activation-Scripts/issues).

> [!NOTE]
>
> - The `irm` command in PowerShell downloads a script from a specified URL, and the `iex` command executes it.
> - Always double-check the URL before executing the command and verify the source is trustworthy when manually downloading files.
> - Be cautious of third parties spreading malware disguised as MAS by altering the URL in the PowerShell command.

---
