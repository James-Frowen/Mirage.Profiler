#!/bin/bash

echo "Running release script with [SOURCE_PATH=${SOURCE_PATH}, TARGET_PATH=${TARGET_PATH}, args=$@]"

VER=$(echo $1 | sed 's/-[a-z]*//g')
sed -i -e '/AssemblyVersion/s/\".*\"/\"'$VER'\"/' \
    Assets/Mirage.Profiler/AssemblyInfo.cs \
    Assets/Mirage.Profiler/Editor/AssemblyInfo.cs

unity-packer pack Mirage.Profiler.unitypackage \
    ${SOURCE_PATH} ${TARGET_PATH} \
    LICENSE ${TARGET_PATH}/LICENSE \
    README.md ${TARGET_PATH}/README.md \
    CHANGELOG.md ${TARGET_PATH}/CHANGELOG.md
