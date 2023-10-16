# Ara 3D Dev 

Main development repository for all public Ara 3D projects. 
Synchronizes development across multiple repositories. 

## Helper Scripts 

The helper script `bin\gap.bat <message>` combines multiple git commands
allowing you to add, commit, and push in one command. 
The mnemonic is that it stands for "git add push". 

The helper script `bin\gaps.bat <message>` combines 
a `gap.bat` command to the subrepository
that you are working on, and to the main repository as well.  
The mnemonic is that it stands for "git add push submodule".

The helper script `bin\gapa.bat <message>` calls `gap.bat <message>`
for all submodules at once. 

It is recommended to add the `bin` folder of this repository to your path. 

## Submodules

This development repository tracks multiple separate repositories as submodules. 
After you first clone this repo, you must retrieve all submodules. 

  `git submodule update --init --recursive`

## Updating Nuget Package Source 

In order to work from this repository you need to update your nuget package source to build from the local nuget folder.

![image](https://github.com/ara3d/ara3d-dev/assets/1759994/940036b6-cea2-4e34-833e-b4a36630b1fc)

