注意：程序目前只实现了单播(unicast)交互
## 单播接口
建立TCP连接后，进行如下交互操作
### 1. DESCRIBE

客户端发起`DESCRIBE`
```
DESCRIBE rtsp://127.0.0.1/PLTV/demo.smil RTSP/1.0
CSeq: 2
User-Agent: Lavf58.20.100
Accept: application/sdp
```
收到`302`响应
```
RTSP/1.0 302 Moved Temporarily
Location: rtsp://127.0.0.1/PLTV/2423234/00000/demo.smil RTSP/1.0
Date: Fri, 12 Nov 2021 08:53:13 GMT
CSeq: 2
Server: HWServer/1.0.0.1
```
使用新的URL重新发起`DESCRIBE`操作
```
DESCRIBE rtsp://127.0.0.1/PLTV/2423234/00000/demo.smil RTSP/1.0
CSeq: 2
User-Agent: Lavf58.20.100
Accept: application/sdp
```
响应头出现`Content-Base`，使用该值进行后续请求
```
RTSP/1.0 200 OK
Server: HMS_V1R2
CSeq: 2
Date: Fri, 12 Nov 2021 08:53:14 GMT
Session: 2688054511
Timeshift-Status: 1
Content-Length: 135
Content-Type: application/sdp
Content-Base: rtsp://127.0.0.1/PLTV/2423234/00000/demo.smil/

v=0
o=- 1702415089 4281335390 IN IP4 127.0.0.1
s=live
t=0 0
c=IN IP4 0.0.0.0
a=range:clock=0-
m=video 0 MP2T/AVP 33
b=AS:15858
```
### 2. SETUP

客户端发起`SETUP`
```
SETUP rtsp://127.0.0.1/PLTV/2423234/00000/demo.smil/ RTSP/1.0
Transport: MP2T/TCP;unicast;interleaved=0-1
CSeq: 3
User-Agent: Lavf58.20.100
```
收到响应
```
RTSP/1.0 200 OK
Server: HMS_V1R2
CSeq: 3
Date: Fri, 12 Nov 2021 08:53:14 GMT
Session: 2688054511
Timeshift-Status: 1
Transport: MP2T/TCP;unicast;interleaved=0-1;source=183.59.174.38
```
通常，IPTV服务器支持四种模式：
* `MP2T/TCP` 使用TCP直接承载`MPEG-TS`流
* `MP2T/UDP` 使用UDP直接承载`MPEG-TS`流
* `MP2T/RTP/TCP` 使用基于TCP的RTP承载`MPEG-TS`流
* `MP2T/RTP/UDP` 使用基于UDP的RTP承载`MPEG-TS`流

本程序设置为`MP2T/TCP`模式，即使用TCP直接承载`MPEG-TS`流，此模式下的数据处理最为简单

### 3. PLAY

客户端发起`PLAY`
```
PLAY rtsp://127.0.0.1/PLTV/2423234/00000/demo.smil/ RTSP/1.0
Range: npt=0.000-
CSeq: 4
User-Agent: Lavf58.20.100
```
收到响应
```
RTSP/1.0 200 OK
Server: HMS_V1R2
CSeq: 4
Date: Fri, 12 Nov 2021 08:53:14 GMT
Session: 2688054511
Timeshift-Status: 1
Scale: 1.0
```
### 4. 数据处理
客户端发起`PLAY`后，客户端可以从服务端开始读取数据。在`MP2T/TCP`模式下，数据构成如下：
```
           ___________________
TCP Header | 4 Bits          | TS Data (7*188)
           | $ | id | Length |
           -------------------
```
在TCP头部后，第一个字节为`$（0x24）`，第二个字节为`interleave id`，再后两个字节为4字节后紧跟的TS数据包长度，一般为7*188=1316

程序只保留`interleave id`为`0`的数据，其他丢弃即可

## 参考文档
* 中国联通家庭宽带多媒体应用业务平台技术规范与机顶盒终端接口分册（V0.3）