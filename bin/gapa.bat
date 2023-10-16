@echo off
pushd %~dp0\..
for /D %%d in (.\submodules\*) do (
    cd %%d
    gaps %1
)
cd ..
gap %1
popd