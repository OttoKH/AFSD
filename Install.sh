#!/bin/bash
apt update
sudo wget https://download.visualstudio.microsoft.com/download/pr/1b2a2fb1-b04b-485b-8a25-caed97ebe601/0c6024a3814f664558dc39fc85b34192/dotnet-sdk-3.1.416-linux-arm.tar.gz
sudo mkdir -p $HOME/dotnet
sudo tar zxf dotnet-sdk-3.1.416-linux-arm.tar.gz -C $HOME/dotnet
sudo rm /etc/xdg/lxsession/LXDE-pi/sshpwd.sh

sudo apt install -y unclutter libx11-dev libvlc-dev

sudo mv /etc/xdg/lxsession/LXDE-pi/autostart' '/etc/xdg/lxsession/LXDE-pi/autostart.backup
sudo touch /etc/xdg/lxsession/LXDE-pi/autostart
sudo echo '#@lxpanel --profile LXDE-pi' >> /etc/xdg/lxsession/LXDE-pi/autostart
sudo echo '@pcmanfm --desktop --profile LXDE-pi' >> /etc/xdg/lxsession/LXDE-pi/autostart
sudo echo '@xscreensaver -no-splash' >> /etc/xdg/lxsession/LXDE-pi/autostart
sudo echo 'unclutter -idle 0' >> /etc/xdg/lxsession/LXDE-pi/autostart
sudo echo '@sudo /bin/bash /home/pi/AFSD/Run_On_Boot.sh' >> /etc/xdg/lxsession/LXDE-pi/autostart
sudo echo '@pcmanfm --desktop --profile LXDE-pi' >> /etc/xdg/lxsession/LXDE-pi/autostart

sudo rm -rf $HOME/LCD-show
cd $HOME/
sudo git clone https://github.com/goodtft/LCD-show.git
sudo chmod -R 755 $HOME/LCD-show
cd $HOME/LCD-show/
sudo ./MPI3508-show 270


#!/bin/bash
apt update

wget https://download.visualstudio.microsoft.com/download/pr/1b2a2fb1-b04b-485b-8a25-caed97ebe601/0c6024a3814f664558dc39fc85b34192/dotnet-sdk-3.1.416-linux-arm.tar.gz
sudo mkdir -p /home/pi/dotnet
sudo tar zxf dotnet-sdk-3.1.416-linux-arm.tar.gz -C /home/pi/dotnet
sudo echo export DOTNET_ROOT=$HOME/dotnet >> .bashrc
sudo echo export PATH=$PATH:$HOME/dotnet >> .bashrc

rm /etc/xdg/lxsession/LXDE-pi/sshpwd.sh

apt install -y unclutter libx11-dev libvlc-dev

rm -rf LCD-show
git clone https://github.com/goodtft/LCD-show.git
chmod -R 755 LCD-show
cd LCD-show/
sudo ./MPI3050-show 270











