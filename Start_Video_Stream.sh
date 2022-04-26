#!/bin/bash
cd /home/pi/server
#./rtsp-simple-server &
sleep 2
while true
do
#libcamera-vid -t 0 --roi 0.25,0.25,0.5,0.5 --width 1920 --height 1080 --framerate 5 --inline -o - | cvlc -vvv stream:///dev/stdin --sout '#rtp{sdp=rtsp://:8080/}' :demux=h264
#libcamera-vid -t 0 --roi 0.3,0.25,0.45,0.5 --width 320 --height 320 --rotation 180 --framerate 5 --inline -o - | cvlc -vvv stream:///dev/stdin --sout '#rtp{sdp=rtsp://:8080/}' :demux=h264
#libcamera-vid -t 0 --roi 0.25,0.25,0.5,0.5 --width 1920 --height 1080 --framerate 5 --inline -o - | ffmpeg -f flv rtmp://127.0.0.1/live/stream
#ffmpeg -f video4linux2 -input_format h264 -video_size 1280x720 -framerate 30 -i /dev/video0 -f flv rtmp://127.0.0.1/live/stream
#libcamera-vid -t 0 --roi 0.25,0.25,0.5,0.5 --width 1920 --height 1280 --framerate 5 --inline -o - | ffmpeg -i - -f flv http://127.0.0.1/live/stream
#libcamera-vid -t 0 --roi 0.3,0.25,0.45,0.5 --width 320 --height 320 --rotation 180 --framerate 5 --inline -o - | ffmpeg -i - -f rtsp rtsp://localhost:8554/mystream
#libcamera-vid -t 0 --roi 0.3,0.25,0.5,0.5 --width 1920 --height 1080 --framerate 5 --inline -o - | ffmpeg -i - -f hls hls://localhost:8554/mystream
#libcamera-vid -t 0 --roi 0.3,0.25,0.45,0.5 --width 320 --height 320 --rotation 180 --framerate 24 --listen --inline -o tcp://127.0.0.1:8554
libcamera-vid -t 0 --roi 0.3,0.25,0.45,0.5 --width 320 --height 320 --rotation 180 --framerate 26 --listen --inline -o tcp://127.0.0.1:8554
sleep 50
done