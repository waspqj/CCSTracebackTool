using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace CCS_Traceback
{
    /// <summary>从 ApplicationSetting.json 加载配置路径(MES 日志 / NG 错图)。失败抛 ConfigLoadException。</summary>
    public static class ConfigLoader
    {
        public static ApplicationSettingDto Load(string jsonPath)
        {
            if (string.IsNullOrWhiteSpace(jsonPath))
                throw new ConfigLoadException("路径为空。");
            if (!File.Exists(jsonPath))
                throw new ConfigLoadException($"找不到文件: {jsonPath}");

            string text;
            try { text = File.ReadAllText(jsonPath, Encoding.UTF8); }
            catch (Exception ex) { throw new ConfigLoadException("读取文件失败: " + ex.Message, ex); }
            if (string.IsNullOrWhiteSpace(text))
                throw new ConfigLoadException("文件内容为空。");

            ApplicationSettingDto dto;
            try
            {
                var serializer = new DataContractJsonSerializer(typeof(ApplicationSettingDto));
                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(text)))
                    dto = (ApplicationSettingDto)serializer.ReadObject(ms);
            }
            catch (Exception ex) { throw new ConfigLoadException("JSON 解析失败: " + ex.Message, ex); }
            if (dto == null)
                throw new ConfigLoadException("JSON 解析结果为空。");

            if (string.IsNullOrWhiteSpace(dto.PathMesLog))
                throw new ConfigLoadException("文件里没有 PathMesLog 字段,或字段值为空。");
            if (string.IsNullOrWhiteSpace(dto.NGImgSave))
                throw new ConfigLoadException("文件里没有 NGImgSave 字段,或字段值为空。");

            return dto;
        }
    }

    [System.Runtime.Serialization.DataContract]
    public class ApplicationSettingDto
    {
        [System.Runtime.Serialization.DataMember(Name = "PathMesLog", IsRequired = false, EmitDefaultValue = false)]
        public string PathMesLog { get; set; }

        [System.Runtime.Serialization.DataMember(Name = "NGImgSave", IsRequired = false, EmitDefaultValue = false)]
        public string NGImgSave { get; set; }
    }

    /// <summary>ConfigLoader 加载失败时抛的异常,message 直接给用户看。</summary>
    public class ConfigLoadException : Exception
    {
        public ConfigLoadException(string message) : base(message) { }
        public ConfigLoadException(string message, Exception inner) : base(message, inner) { }
    }
}
