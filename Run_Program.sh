#!bin/bash
sleep 10

while true
do
dotnet /home/pi/AFSD/AFSD.dll
echo "Crashed..."
sleep 4
done
