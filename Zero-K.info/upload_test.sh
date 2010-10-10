#!/bin/bash

WEBDIR=`dirname $0`
rsync -avz --delete --delete-excluded --exclude ".svn" "$WEBDIR/" "maackey@springrts.com:test/"
