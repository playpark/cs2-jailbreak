> [!NOTE]
> **changes**
> * updated configs
> * updated commands
> * made noblock toggle instead of commands for on and off
> * made force open & close doors into one command so it toggles, instead of two commands
> * removed lang lines for warden commands and made a file to store all info (wardencommands.txt)
> * removed repeating lang lines for special day start and end, made all use the same localizer
> * cleaned up code
> * read commits for other changes

> [!IMPORTANT]
> FORK FROM [destoer/Cs2Jailbreak](https://github.com/destoer/Cs2Jailbreak)

<img src="https://github.com/user-attachments/assets/53e486cc-8da4-45ab-bc6e-eb38145aba36" height="200px"> <br>

<br> <a href="https://ko-fi.com/exkludera" target="blank"><img src="https://cdn.ko-fi.com/cdn/kofi5.png" height="48px" alt="Buy Me a Coffee at ko-fi.com"></a>

### old readme below

<hr>

# Cs2Jailbreak
WIP Jailbreak plugin for CS2 using counterstrikesharp expect bugs

rewrite of CSS plugin https://github.com/destoer/counter_strike_jailbreak 

admin commands locked under @css/generic 

debug commands locked under @jail/debug


# Info
For further config and feature info please see the wiki

https://github.com/destoer/Cs2Jailbreak/wiki

also please see https://github.com/destoer/JailServiceTest for example API usage

# Installation
Download the latest release from the releases tab and copy it into the counterstrikesharp plugin folder

If you wan't saved LR stats please create a SQL database with a name of your choice and setup the credentials in
configs/plugins/Cs2Jailbreak

NOTE: this plugin only operates correctly on linux

windows has a couple of places where a !IsWindows() check wraps buggy code
namely OnTakeDamage

# Warden TODO
T laser 

Auto unstuck 

Handicap 

Warden Ring 

# LR TODO
Port crash, sumo 

Improve anti cheat

Add beacons 


# SD TODO
waiting on trace for laser wars

Zombie, Laser wars


# Useful thirdparty plugins for jailbreak

CT Bans
https://github.com/DeadSwimek/cs2-ctban

Call admin
https://github.com/1Mack/CS2-CallAdmin

Roll the dice
https://github.com/Quantor97/CS2-RollTheDice-Plugin
