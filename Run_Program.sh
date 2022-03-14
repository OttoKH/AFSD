#!/bin/bash
sleep 10

while true
do
/home/pi/dotnet/dotnet /home/pi/AFSD/AFSD.dll
echo "Program Crashed. Restarting..."
sleep 5
done