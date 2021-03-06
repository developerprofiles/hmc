﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using DSWrapper;
using System.Threading;

namespace HomeMediaCenter
{
    public class DirectShowEncoder : EncoderBuilder
    {
        private enum DSCodec { MPEG2_PS, MPEG2LAYER1_PS, MPEG2_TS, MPEG2LAYER1_TS, MPEG2_TS_H264, WEBM, WEBM_TS, WMV2, WMV3, AVI, MP4, MP4_TS, MP3, MP3_TS, FLV, FLV_TS, FLV_H264, FLV_H264_TS, HLS }

        private DSEncoder encoder;
        private DSCodec codec;

        protected DirectShowEncoder() { }

        internal static DirectShowEncoder TryCreate(Dictionary<string, string> parameters)
        {
            DSCodec codecEnum;
            if (Enum.TryParse<DSCodec>(parameters["codec"], true, out codecEnum))
            {
                DirectShowEncoder encoder = new DirectShowEncoder();
                encoder.codec = codecEnum;
                encoder.parameters = parameters;
                return encoder;
            }

            return null;
        }

        public static bool IsFFDSHOWInstalled()
        {
            return ContainerType.IsFFDSHOWInstalled();
        }

        public static bool IsWMVInstalled()
        {
            return ContainerType.IsWMVInstalled();
        }

        public static bool IsMPEG2Installed()
        {
            return ContainerType.IsMPEG2Installed();
        }

        public static bool IsHMCInstalled()
        {
            return ContainerType.IsHMCInstalled();
        }

        public static bool IsLAVSplitInstalled()
        {
            return ContainerType.IsLAVSplitInstalled();
        }

        public static string[] GetVideoInputNames()
        {
            return WebcamInput.GetVideoInputNames().Select(a => "Webcam_" + a).ToArray();
        }

        public override string GetMime()
        {
            switch (this.codec)
            {
                case DSCodec.MPEG2_PS: return (this.video.HasValue && this.video.Value == 0) ? "audio/mpeg" : "video/mpeg";
                case DSCodec.MPEG2_TS: goto case DSCodec.MPEG2_PS;
                case DSCodec.MPEG2LAYER1_PS: goto case DSCodec.MPEG2_PS;
                case DSCodec.MPEG2LAYER1_TS: goto case DSCodec.MPEG2_PS;
                case DSCodec.MPEG2_TS_H264: goto case DSCodec.MPEG2_PS;
                case DSCodec.WEBM: return (this.video.HasValue && this.video.Value == 0) ? "audio/webm" : "video/webm";
                case DSCodec.WEBM_TS: goto case DSCodec.WEBM;
                case DSCodec.WMV2: return (this.video.HasValue && this.video.Value == 0) ? "audio/x-ms-wma" : "video/x-ms-wmv";
                case DSCodec.WMV3: goto case DSCodec.WMV2;
                case DSCodec.MP3: return "audio/mpeg";
                case DSCodec.MP3_TS: goto case DSCodec.MP3;
                case DSCodec.MP4: return "video/mp4";
                case DSCodec.MP4_TS: goto case DSCodec.MP4;
                case DSCodec.AVI: return "video/avi";
                case DSCodec.FLV: return "video/x-flv";
                case DSCodec.FLV_TS: goto case DSCodec.FLV;
                case DSCodec.FLV_H264: goto case DSCodec.FLV;
                case DSCodec.FLV_H264_TS: goto case DSCodec.FLV;
                default: return string.Empty;
            }
        }

        public override string GetDlnaType()
        {
            switch (this.codec)
            {
                case DSCodec.MPEG2_PS: return (this.video.HasValue && this.video.Value == 0) ? string.Empty : "DLNA.ORG_PN=MPEG_PS_PAL;";
                case DSCodec.MPEG2_TS: return (this.video.HasValue && this.video.Value == 0) ? string.Empty : "DLNA.ORG_PN=MPEG_TS_SD_EU_ISO;";
                case DSCodec.MPEG2LAYER1_PS: goto case DSCodec.MPEG2_PS;
                case DSCodec.MPEG2LAYER1_TS: goto case DSCodec.MPEG2_TS;
                case DSCodec.MPEG2_TS_H264: goto case DSCodec.MPEG2_TS;
                case DSCodec.WMV2: return (this.video.HasValue && this.video.Value == 0) ? "DLNA.ORG_PN=WMABASE;" : "DLNA.ORG_PN=WMVMED_BASE;";
                case DSCodec.WMV3: goto case DSCodec.WMV2;
                case DSCodec.MP3: return "DLNA.ORG_PN=MP3;";
                case DSCodec.MP3_TS: goto case DSCodec.MP3;
                default: return string.Empty;
            }
        }

        public override void StopEncode()
        {
            DSEncoder enc = this.encoder;
            if (enc != null)
                enc.StopEncode();
        }

        public override void StartEncode(Stream output)
        {
            using (DSEncoder enc = new DSEncoder())
            {
                this.encoder = enc;

                string source = parameters["source"];
                if (source.StartsWith("desktop", StringComparison.OrdinalIgnoreCase))
                {
                    //Zdroj je plocha PC
                    uint desktopFps = parameters.ContainsKey("desktopfps") ? uint.Parse(parameters["desktopfps"]) : 10;
                    bool captureWin = string.Equals(source, "desktop_window", StringComparison.OrdinalIgnoreCase);

                    enc.SetInput(InputType.Desktop(desktopFps, captureWin));
                }
                else if (string.Equals(source, "static", StringComparison.OrdinalIgnoreCase))
                {
                    //Zdrojom je staticky obraz
                    using (MemoryStream ms = new MemoryStream())
                    using (System.Drawing.Bitmap bitmap = HelperClass.GetTextBitmap(LanguageResource.Loading + "...", 720, 576, 25))
                    {
                        bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                        enc.SetInput(InputType.Static(10, ms.ToArray()));
                    }
                }
                else if (source.StartsWith("webcam", StringComparison.OrdinalIgnoreCase))
                {
                    //Zdroj je webkamera
                    string[] webcamParam = source.Split(new char[] { '_' }, 2, StringSplitOptions.RemoveEmptyEntries);
                    if (webcamParam.Length == 1)
                        enc.SetInput(InputType.Webcam(WebcamInput.GetVideoInputNames().FirstOrDefault(), WebcamInput.GetAudioInputNames().FirstOrDefault()));
                    else
                        enc.SetInput(InputType.Webcam(webcamParam[1], WebcamInput.GetAudioInputNames().FirstOrDefault()));
                }
                else
                {
                    //Zdrj je subor, http link
                    bool reqSeeking = (parameters.ContainsKey("starttime") || parameters.ContainsKey("endtime")) &&
                        this.codec != DSCodec.MPEG2_PS && this.codec != DSCodec.MPEG2_TS &&
                        this.codec != DSCodec.MPEG2LAYER1_PS && this.codec != DSCodec.MPEG2LAYER1_TS;

                    //Nastavi sa preferovany demultiplexor podla guid, hodnoty su oddelene znakom |
                    IEnumerable<Guid> prefDsDemux = null;
                    if (parameters.ContainsKey("prefDsDemux"))
                        prefDsDemux = parameters["prefDsDemux"].Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries).Select(a => Guid.Parse(a));

                    enc.SetInput(source, reqSeeking, prefDsDemux);
                }

                uint setVideo = this.video.HasValue ? this.video.Value : 1;
                uint setAudio = this.audio.HasValue ? this.audio.Value : 1;

                //Integrovanie titulkov, ak nie je zadany nazov - automaticka detekcia
                //Ak je zadany nazov - nastav prislusny subor
                string subPath = null;
                bool subtitles = parameters.ContainsKey("subtitles");
                if (subtitles)
                {
                    if (parameters["subtitles"] != string.Empty)
                        subPath = parameters["subtitles"];
                }

                //Nastavenie video a zvukovej stopy
                uint actVideo = 1, actAudio = 1, actSubtitle = 1;
                foreach (PinInfoItem item in enc.SourcePins)
                {
                    if (item.MediaType == PinMediaType.Video)
                    {
                        //Ak sa jedna o stream a uzivatel nezadal index, nenastavuje sa IsSelected - necha sa povodne
                        if (item.IsStream && !this.video.HasValue)
                            continue;

                        if (actVideo == setVideo)
                            item.IsSelected = true;
                        else
                            item.IsSelected = false;
                        actVideo++;
                    }
                    else if (item.MediaType == PinMediaType.Audio)
                    {
                        //Ak sa jedna o stream a uzivatel nezadal index, nenastavuje sa IsSelected - necha sa povodne
                        if (item.IsStream && !this.audio.HasValue)
                            continue;

                        if (actAudio == setAudio)
                            item.IsSelected = true;
                        else
                            item.IsSelected = false;
                        actAudio++;
                    }
                    else if (item.MediaType == PinMediaType.Subtitle)
                    {
                        //Ak uzivatel nevyziadal titulky - nezapnu sa ani ked ide stream
                        string lang = ((PinSubtitleItem)item).LangName;
                        if (string.Equals(lang, subPath, StringComparison.OrdinalIgnoreCase) || actSubtitle.ToString() == subPath)
                        {
                            //Ak parameter zodpoveda nazvu jazyka alebo indexu nastavi sa IsSelected
                            //subPath sa nastavi na null - aby ho enkoder nepovazoval ze cestu k titulkom
                            subPath = null;
                            item.IsSelected = true;
                        }
                        else
                        {
                            item.IsSelected = false;
                        }
                        actSubtitle++;
                    }
                    else
                    {
                        item.IsSelected = false;
                    }
                }

                //Zistenie sirky a vysky, povodna hodnota ak nezadane
                uint width = this.width.HasValue ? this.width.Value : 0;
                uint height = this.height.HasValue ? this.height.Value : 0;

                //Zistenie bitrate pre audio a vide, povodna hodnota ak nezadane
                uint vidBitrate = this.vidBitrate.HasValue ? this.vidBitrate.Value : 0;
                uint audBitrate = this.audBitrate.HasValue ? this.audBitrate.Value : 0;

                //Zistenie kvality videa
                uint quality = 50;
                if (parameters.ContainsKey("quality"))
                {
                    quality = uint.Parse(parameters["quality"]);
                    if (quality < 1 || quality > 100)
                        throw new HttpException(400, "Encode quality must be in range 1-100");
                }

                //Zistenie poctu snimok za sekundu
                uint fps = 0;
                if (parameters.ContainsKey("fps"))
                    fps = uint.Parse(parameters["fps"]);

                //Zistenie ci zachovat pomer stran pri zmene rozlisenia
                bool keepAspect = parameters.ContainsKey("keepaspect");

                //Zistenie typu skenovania
                ScanType? scanType = null;
                if (parameters.ContainsKey("scan"))
                {
                    if (parameters["scan"].Equals("i", StringComparison.OrdinalIgnoreCase))
                        scanType = ScanType.Interlaced;
                    else if (parameters["scan"].Equals("p", StringComparison.OrdinalIgnoreCase))
                        scanType = ScanType.Progressive;
                }

                //Nastavenie velkosti buffera (KByte) pre async stream - mozne iba pre non-seekable container
                int outBufferSize = 0;
                if (parameters.ContainsKey("obufsize"))
                {
                    outBufferSize = int.Parse(parameters["obufsize"]);
                    if (outBufferSize < 1)
                        throw new HttpException(400, "Output buffer size must be non-negative");
                    outBufferSize *= 1024;
                }

                ContainerType container = null;
                switch (this.codec)
                {
                    case DSCodec.MPEG2_PS: container = ContainerType.MPEG2_PS(width, height, BitrateMode.CBR, vidBitrate * 1000, quality, fps, 
                        scanType.HasValue ? scanType.Value : ScanType.Interlaced, subtitles, subPath, keepAspect, MpaLayer.Layer2, audBitrate * 1000);
                        break;
                    case DSCodec.MPEG2_TS: container = ContainerType.MPEG2_TS(width, height, BitrateMode.CBR, vidBitrate * 1000, quality, fps,
                        scanType.HasValue ? scanType.Value : ScanType.Interlaced, subtitles, subPath, keepAspect, MpaLayer.Layer2, audBitrate * 1000);
                        break;
                    case DSCodec.MPEG2LAYER1_PS: container = ContainerType.MPEG2_PS(width, height, BitrateMode.CBR, vidBitrate * 1000, quality, fps,
                        scanType.HasValue ? scanType.Value : ScanType.Interlaced, subtitles, subPath, keepAspect, MpaLayer.Layer1, audBitrate * 1000);
                        break;
                    case DSCodec.MPEG2LAYER1_TS: container = ContainerType.MPEG2_TS(width, height, BitrateMode.CBR, vidBitrate * 1000, quality, fps,
                        scanType.HasValue ? scanType.Value : ScanType.Interlaced, subtitles, subPath, keepAspect, MpaLayer.Layer1, audBitrate * 1000);
                        break;
                    case DSCodec.MPEG2_TS_H264: container = ContainerType.MPEG2_TS_H264(width, height, BitrateMode.CBR, vidBitrate * 1000, quality, fps,
                        subtitles, subPath, keepAspect, audBitrate * 1000);
                        break;
                    case DSCodec.HLS:
                        if (!parameters.ContainsKey("hlsPlaylistUrl") || !parameters.ContainsKey("hlsFileUrl") || !parameters.ContainsKey("hlsSegmentTime"))
                            throw new HttpException(400, "Missing 'hlsPlaylistUrl', 'hlsFileUrl' or 'hlsSegmentTime' parameter");

                        container = ContainerType.HLS(width, height, BitrateMode.CBR, vidBitrate * 1000, quality, fps, subtitles, subPath, keepAspect,
                            audBitrate * 1000, parameters["hlsPlaylistUrl"], parameters["hlsFileUrl"], uint.Parse(parameters["hlsSegmentTime"]));
                        outBufferSize = 0;
                        //HLS nepouzivat ako vystup stream
                        output = null;
                        break;
                    case DSCodec.WEBM: container = ContainerType.WEBM(width, height, BitrateMode.CBR, vidBitrate * 1000, quality, fps, subtitles, 
                        subPath, keepAspect, audBitrate * 1000, false);
                        outBufferSize = 0;
                        break;
                    case DSCodec.WEBM_TS: container = ContainerType.WEBM(width, height, BitrateMode.CBR, vidBitrate * 1000, quality, fps, subtitles,
                        subPath, keepAspect, audBitrate * 1000, true);
                        break;
                    case DSCodec.WMV2: container = ContainerType.WMV(width, height, WMVideoSubtype.WMMEDIASUBTYPE_WMV2, vidBitrate * 1000, quality, 
                        fps, subtitles, subPath, audBitrate * 1000);
                        break;
                    case DSCodec.WMV3: container = ContainerType.WMV(width, height, WMVideoSubtype.WMMEDIASUBTYPE_WMV3, vidBitrate * 1000, quality,
                        fps, subtitles, subPath, audBitrate * 1000);
                        break;
                    case DSCodec.AVI: container = ContainerType.AVI(width, height, BitrateMode.CBR, vidBitrate * 1000, quality, fps,
                        scanType.HasValue ? scanType.Value : ScanType.Progressive, subtitles, subPath, keepAspect, audBitrate * 1000);
                        outBufferSize = 0;
                        break;
                    case DSCodec.MP4: container = ContainerType.MP4(width, height, BitrateMode.CBR, vidBitrate * 1000, quality, fps, subtitles, subPath, 
                        keepAspect, audBitrate * 1000, false);
                        outBufferSize = 0;
                        break;
                    case DSCodec.MP4_TS: container = ContainerType.MP4(width, height, BitrateMode.CBR, vidBitrate * 1000, quality, fps, subtitles, subPath,
                        keepAspect, audBitrate * 1000, true);
                        break;
                    case DSCodec.MP3: container = ContainerType.MP3(BitrateMode.CBR, audBitrate * 1000, quality, false);
                        outBufferSize = 0;
                        break;
                    case DSCodec.MP3_TS: container = ContainerType.MP3(BitrateMode.CBR, audBitrate * 1000, quality, true);
                        break;
                    case DSCodec.FLV: container = ContainerType.FLV(width, height, BitrateMode.CBR, vidBitrate * 1000, quality, fps, subtitles, subPath,
                        keepAspect, audBitrate * 1000, false);
                        outBufferSize = 0;
                        break;
                    case DSCodec.FLV_TS: container = ContainerType.FLV(width, height, BitrateMode.CBR, vidBitrate * 1000, quality, fps, subtitles, subPath,
                        keepAspect, audBitrate * 1000, true);
                        break;
                    case DSCodec.FLV_H264: container = ContainerType.FLV_H264(width, height, BitrateMode.CBR, vidBitrate * 1000, quality, fps, subtitles, subPath,
                        keepAspect, audBitrate * 1000, false);
                        outBufferSize = 0;
                        break;
                    case DSCodec.FLV_H264_TS: container = ContainerType.FLV_H264(width, height, BitrateMode.CBR, vidBitrate * 1000, quality, fps, subtitles, subPath,
                        keepAspect, audBitrate * 1000, true);
                        break;
                }

                //Nastavenie casu zaciatku a konca v milisekundach (v parametroch su sekundy)
                long startTime = 0, endTime = 0;
                if (parameters.ContainsKey("starttime"))
                    startTime = (long)(double.Parse(parameters["starttime"], System.Globalization.CultureInfo.InvariantCulture) * 1000);
                if (parameters.ContainsKey("endtime"))
                    endTime = (long)(double.Parse(parameters["endtime"], System.Globalization.CultureInfo.InvariantCulture) * 1000);

                if (this.progressChangeDel != null)
                    enc.ProgressChange += new EventHandler<ProgressChangeEventArgs>(enc_ProgressChange);

                using (BufferedAsyncStream bufferedOutput = new BufferedAsyncStream(output, outBufferSize))
                {
                    if (outBufferSize > 0)
                    {
                        bufferedOutput.Start();
                        output = bufferedOutput;
                    }

                    //Nastavenie vystupu
                    enc.SetOutput(output, container, startTime, endTime);

                    enc.StartEncode();

                    bufferedOutput.Stop();
                }
            }
        }

        private void enc_ProgressChange(object sender, ProgressChangeEventArgs e)
        {
            this.progressChangeDel(e.Progress);
        }
    }
}
