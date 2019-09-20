#!/usr/bin/env bash

ANDROID_DIR=$1

cd $ANDROID_DIR && 
brew install gradle && 
 gradle wrapper && 
 source ~/.bash_profile && 
 cd $(echo $ANDROID_SDK) && 
 cd ./tools/ && 
(emulator @hackathon & (sleep 10 && echo "YAWN" && adb -e install -r "$ANDROID_DIR/app/build/outputs/apk/debug/app-debug.apk" && 
adb shell am start -a android.intent.action.MAIN -n com.sempiler.hackathon/foo.MainActivity))