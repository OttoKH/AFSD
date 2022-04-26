#!bin/bash
sleep 10
sudo service AFSD_Stream start
while true
do
/home/pi/dotnet/dotnet /home/pi/AFSD/AFSD.dll
echo "Crashed..."
sleep 4
done
