#!/usr/bin/env bash

NOW_DIR=$1
PORT=$2

osascript -e "tell application \"Terminal\" to do script \"cd $NOW_DIR && npm i && now dev -p $PORT\""