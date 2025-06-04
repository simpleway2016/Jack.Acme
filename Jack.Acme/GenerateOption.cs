using System;
using System.Collections.Generic;
using System.Text;

namespace Jack.Acme
{
    public class GenerateOption
    {
        /// <summary>
        /// 证书存访的文件夹路径，默认当前程序所在文件夹
        /// </summary>
        public string SaveFolder { get; set; }

        public string GetSaveFolderPath()
        {
            if (string.IsNullOrWhiteSpace(this.SaveFolder))
                return "";
            else if (this.SaveFolder.EndsWith("/") || this.SaveFolder.EndsWith("\\"))
                return SaveFolder;
            else
                return $"{SaveFolder}/";
        }
    }
}
