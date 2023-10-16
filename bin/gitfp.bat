git add .
git commit -m %1
git pushd
pushd %~dp0\..
git add .
git commit -m %1
git push
popd 