#!bin/bash
BASHFILE=/home/pi/AFSD/LiveCMD.sh
sleep 10
sudo chmod 777 /home/pi/AFSD/Take_Picture.sh
lxterminal -e "bash /home/pi/AFSD/Run_Program.sh" &
while true
do
sudo chmod 777 /home/pi/AFSD --recursive
#[ -x /home/pi/AFSD/LiveCMD.sh ] && bash /home/pi/AFSD/LiveCMD.sh
#echo "$BASHFILE"
if [ -e '/home/pi/AFSD/LiveCMD.sh' ]; then
	bash "/home/pi/AFSD/LiveCMD.sh"
#else
	#echo "No file"
fi
sleep 1
done
