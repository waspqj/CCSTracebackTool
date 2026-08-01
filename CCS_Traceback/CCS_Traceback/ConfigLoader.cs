using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace CCS_Traceback
{
    /// <summary>
    /// 从 ApplicationSetting.json 加载配置路径(MES 日志 / 检测图 / NG 错图)。
    ///
    /// 反序列化用的 DTO 只取这个工具真正用到的字段,
    /// 不去复刻 CCS_AOI 项目的 AppConfig 全字段。
    /// 加载失败一律抛 ConfigLoadException,由调用方决定怎么弹错。
    /// </summary>
    public static class ConfigLoader
    {
        /// <summary>
        /// 加载整个 DTO。失败抛 ConfigLoadException。
        /// 只校验两个必填字段(PathMesLog / NGImgSave)。
        /// </summary>
        public static ApplicationSettingDto Load(string jsonPath)
        {
            if (string.IsNullOrWhiteSpace(jsonPath))
                throw new ConfigLoadException("路径为空。");

            if (!File.Exists(jsonPath))
                throw new ConfigLoadException($"找不到文件: {jsonPath}");

            string text;
            try
            {
                text = File.ReadAllText(jsonPath, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                throw new ConfigLoadException("读取文件失败: " + ex.Message, ex);
            }

            if (string.IsNullOrWhiteSpace(text))
                throw new ConfigLoadException("文件内容为空。");

            ApplicationSettingDto dto;
            try
            {
                var serializer = new DataContractJsonSerializer(typeof(ApplicationSettingDto));
                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(text)))
                {
                    dto = (ApplicationSettingDto)serializer.ReadObject(ms);
                }
            }
            catch (Exception ex)
            {
                throw new ConfigLoadException("JSON 解析失败: " + ex.Message, ex);
            }

            if (dto == null)
                throw new ConfigLoadException("JSON 解析结果为空。");

            // 两个字段必须都存在 —— MES 日志读 CSV + NG 错图扫描都要
            if (string.IsNullOrWhiteSpace(dto.PathMesLog))
                throw new ConfigLoadException("文件里没有 PathMesLog 字段,或字段值为空。");
            if (string.IsNullOrWhiteSpace(dto.NGImgSave))
                throw new ConfigLoadException("文件里没有 NGImgSave 字段,或字段值为空。");

            return dto;
        }
    }

    /// <summary>
    /// ApplicationSetting.json 里这个工具需要的字段。
    /// DataContractJsonSerializer 要求:可选字段 + 默认值,且需要 DataContract/DataMember 标记。
    /// </summary>
    [System.Runtime.Serialization.DataContract]
    public class ApplicationSettingDto
    {
        [System.Runtime.Serialization.DataMember(Name = "PathMesLog", IsRequired = false, EmitDefaultValue = false)]
        public string PathMesLog { get; set; }

        /// <summary>NG 错图根目录(NGImgSave)。</summary>
        [System.Runtime.Serialization.DataMember(Name = "NGImgSave", IsRequired = false, EmitDefaultValue = false)]
        public string NGImgSave { get; set; }
    }

    /// <summary>
    /// ConfigLoader 加载失败时抛的异常,message 直接给用户看。
    /// </summary>
    public class ConfigLoadException : Exception
    {
        public ConfigLoadException(string message) : base(message) { }
        public ConfigLoadException(string message, Exception inner) : base(message, inner) { }
    }
}