#!/bin/bash
apt update
wget https://download.visualstudio.microsoft.com/download/pr/1b2a2fb1-b04b-485b-8a25-caed97ebe601/0c6024a3814f664558dc39fc85b34192/dotnet-sdk-3.1.416-linux-arm.tar.gz
sudo mkdir -p $HOME/dotnet
sudo tar zxf dotnet-sdk-3.1.416-linux-arm.tar.gz -C $HOME/dotnet
sudo rm /etc/xdg/lxsession/LXDE-pi/sshpwd.sh

sudo apt install -y unclutter libx11-dev libvlc-dev

sudo mv /etc/xdg/lxsession/LXDE-pi/autostart /etc/xdg/lxsession/LXDE-pi/autostart.backup
sudo touch /etc/xdg/lxsession/LXDE-pi/autostart
echo '#@lxpanel --profile LXDE-pi' >> /etc/xdg/lxsession/LXDE-pi/autostart
echo '@pcmanfm --desktop --profile LXDE-pi' >> /etc/xdg/lxsession/LXDE-pi/autostart
echo '@xscreensaver -no-splash' >> /etc/xdg/lxsession/LXDE-pi/autostart
echo 'unclutter -idle 0' >> /etc/xdg/lxsession/LXDE-pi/autostart
echo '@sudo /bin/bash /home/pi/AFSD/Run_On_Boot.sh' >> /etc/xdg/lxsession/LXDE-pi/autostart
echo '@pcmanfm --desktop --profile LXDE-pi' >> /etc/xdg/lxsession/LXDE-pi/autostart

sudo rm -rf $HOME/LCD-show
git clone https://github.com/goodtft/LCD-show.git -C $HOME/LCD-show
chmod -R 755 $HOME/LCD-show
cd $HOME/LCD-show/
sudo ./MPI3508-show 270

