# RtspRecorder
适用于 IPTV 的 RTSP 录制命令行工具. 目前仅实现了`MP2T/TCP`传输.

```
RtspRecorder
  适用于 IPTV 的 RTSP 录制工具.

Usage:
  RtspRecorder [options]

Options:
  -i, --input <input> (REQUIRED)  设置输入rtsp://链接.
  -t, --duration <duration>       设置输出长度. [hh:mm:ss]
  -o, --output <output>           设置输出文件. (使用 - 以输出到stdout) [default: auto]
  --program <program>             设置流标题. (当--output为auto时生效) [default: Record]
  --detail                        设置是否输出详细交互信息 [default: False]
  --playback <playback>           设置回看, 格式yyyyMMddHHmmss[-<yyyyMMddHHmmss>]
  --version                       Show version information
  -?, -h, --help                  Show help and usage information
```

# Examples
### 快速上手
```
RtspRecorder -i "rtsp://127.0.0.1/PLTV/demo.smil"
```
程序开始录制TS流并在当前路径写入`Record_yyyy-MM-dd_HH-mm-ss-fff.ts`文件

### 设置输出路径
```
RtspRecorder -i "rtsp://127.0.0.1/PLTV/demo.smil" -o D:\MyRecord\test.ts
```
程序开始录制TS流并写入`D:\MyRecord\test.ts`文件

### 设置流名称
```
RtspRecorder -i "rtsp://127.0.0.1/PLTV/demo.smil" --program "HNTV卫星源码"
```
程序开始录制TS流并在当前路径写入`HNTV卫星源码_yyyy-MM-dd_HH-mm-ss-fff.ts`文件

### 录制20分钟后退出录制
```
RtspRecorder -i "rtsp://127.0.0.1/PLTV/demo.smil" -t 20:00
```

### 从某个时间点开始录制
```
RtspRecorder -i "rtsp://127.0.0.1/PLTV/demo.smil" --playback 20211111190000
```
(此特性可能不被支持) 程序修改RTSP链接以时移至`2021年11月11日19点00分00秒`并开始录制

### 录制指定时间段
```
RtspRecorder -i "rtsp://127.0.0.1/PLTV/demo.smil" --playback 20211111190000-20211111203000
```
(此特性可能不被支持) 程序修改RTSP链接以录制`2021年11月11日19点00分00秒`到`2021年11月11日20点30分00秒`的内容

### 管道输出
```
RtspRecorder -i "rtsp://127.0.0.1/PLTV/demo.smil" -o - | ffmpeg -i - -map 0 -c copy OUTPUT.mp4
```
录制并使用`ffmpeg`封装到`mp4`容器