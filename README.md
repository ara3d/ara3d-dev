# Ara 3D Dev 

Main development repository for all public Ara 3D projects. 
Synchronizes development across multiple repositories. 

## Helper Scripts 

The helper script `bin\gd.bat <message>` combines multiple git commands
allowing you to conveniently push changes to a subrepository
that you are working on.  

1. Stage all work in current repository (assuming you are in a submodule)
2. Commit the work using the commit message
3. Push to the remote repository
4. Change directory to this repository root
5. Stage, commit, and push the changes to this repository
6. Restore the directory 

It is recommended to add the `bin` folder of this repository to your path. 

## Submodules

This development repository tracks multiple separate repositories as submodules. 
After you first clone this repo, you must retrieve all submodules. 

  `git submodule update --init --recursive`

## Updating Nuget Package Source 

In order to work from this repository you need to update your nuget package source to build from the local nuget folder.

![image](https://github.com/ara3d/ara3d-dev/assets/1759994/940036b6-cea2-4e34-833e-b4a36630b1fc)

