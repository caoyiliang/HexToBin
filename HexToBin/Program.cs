namespace HexToBinConverter
{
    public class HexFileConverter
    {
        /// <summary>
        /// 将 HEX 文件转换为 BIN 文件
        /// </summary>
        /// <param name="hexFilePath">输入的 HEX 文件路径</param>
        /// <param name="binFilePath">输出的 BIN 文件路径</param>
        /// <param name="fillByte">用于填充空白地址的字节（默认 0xFF）</param>
        /// <returns>转换是否成功</returns>
        public static bool ConvertHexToBin(string hexFilePath, string binFilePath, byte fillByte = 0xFF)
        {
            try
            {
                Console.WriteLine($"开始转换: {hexFilePath} -> {binFilePath}");

                // 解析 HEX 文件
                var memoryMap = ParseHexFile(hexFilePath);

                if (memoryMap.Count == 0)
                {
                    Console.WriteLine("错误: HEX 文件中未找到有效数据");
                    return false;
                }

                // 获取地址范围并生成 BIN 数据
                var binData = GenerateBinData(memoryMap, fillByte);

                // 写入 BIN 文件
                File.WriteAllBytes(binFilePath, binData);

                Console.WriteLine($"转换成功! 输出文件大小: {binData.Length} 字节");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"转换过程中发生错误: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 解析 HEX 文件
        /// </summary>
        private static Dictionary<UInt32, byte> ParseHexFile(string filePath)
        {
            var memoryMap = new Dictionary<UInt32, byte>();
            var lines = File.ReadAllLines(filePath);
            UInt32 upperAddress = 0; // 用于处理扩展地址

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line[0] != ':')
                    continue;

                try
                {
                    // 解析记录长度
                    int byteCount = Convert.ToInt32(line.Substring(1, 2), 16);

                    // 解析地址
                    UInt32 address = Convert.ToUInt32(line.Substring(3, 4), 16);
                    address += upperAddress; // 添加上层地址

                    // 解析记录类型
                    int recordType = Convert.ToInt32(line.Substring(7, 2), 16);

                    switch (recordType)
                    {
                        case 0x00: // 数据记录
                            ParseDataRecord(line, byteCount, address, memoryMap);
                            break;

                        case 0x01: // 文件结束记录
                            Console.WriteLine("遇到文件结束记录");
                            break;

                        case 0x02: // 扩展段地址记录
                            upperAddress = ParseExtendedSegmentAddress(line) * 16U;
                            Console.WriteLine($"扩展段地址: 0x{upperAddress:X8}");
                            break;

                        case 0x04: // 扩展线性地址记录
                            upperAddress = ParseExtendedLinearAddress(line) << 16;
                            Console.WriteLine($"扩展线性地址: 0x{upperAddress:X8}");
                            break;

                        default:
                            Console.WriteLine($"警告: 忽略未知记录类型 0x{recordType:X2}");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"解析行时出错: {line} - {ex.Message}");
                    throw;
                }
            }

            return memoryMap;
        }

        /// <summary>
        /// 解析数据记录
        /// </summary>
        private static void ParseDataRecord(string line, int byteCount, UInt32 baseAddress,
                                          Dictionary<UInt32, byte> memoryMap)
        {
            int dataStartIndex = 9;

            for (int i = 0; i < byteCount; i++)
            {
                UInt32 currentAddress = baseAddress + (UInt32)i;
                string byteStr = line.Substring(dataStartIndex + i * 2, 2);
                byte dataByte = Convert.ToByte(byteStr, 16);

                memoryMap[currentAddress] = dataByte;
            }
        }

        /// <summary>
        /// 解析扩展段地址记录
        /// </summary>
        private static UInt32 ParseExtendedSegmentAddress(string line)
        {
            // 数据字段包含段地址
            string segmentAddressStr = line.Substring(9, 4);
            return Convert.ToUInt32(segmentAddressStr, 16);
        }

        /// <summary>
        /// 解析扩展线性地址记录
        /// </summary>
        private static UInt32 ParseExtendedLinearAddress(string line)
        {
            // 数据字段包含上层地址
            string upperAddressStr = line.Substring(9, 4);
            return Convert.ToUInt32(upperAddressStr, 16);
        }

        /// <summary>
        /// 生成 BIN 文件数据
        /// </summary>
        private static byte[] GenerateBinData(Dictionary<UInt32, byte> memoryMap, byte fillByte)
        {
            if (memoryMap.Count == 0)
                return new byte[0];

            // 获取最小和最大地址
            var addresses = memoryMap.Keys.ToArray();
            UInt32 minAddress = addresses.Min();
            UInt32 maxAddress = addresses.Max();

            Console.WriteLine($"数据地址范围: 0x{minAddress:X8} - 0x{maxAddress:X8}");
            Console.WriteLine($"数据总大小: {maxAddress - minAddress + 1} 字节");

            // 创建 BIN 数据数组
            int binSize = (int)(maxAddress - minAddress + 1);
            byte[] binData = new byte[binSize];

            // 初始化数组为填充值
            for (int i = 0; i < binSize; i++)
            {
                binData[i] = fillByte;
            }

            // 填充实际数据
            foreach (var kvp in memoryMap)
            {
                UInt32 address = kvp.Key;
                int index = (int)(address - minAddress);

                if (index >= 0 && index < binSize)
                {
                    binData[index] = kvp.Value;
                }
            }

            return binData;
        }
    }

    // 使用示例
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("HEX 转 BIN 文件转换器");
            Console.WriteLine("=====================");

            if (args.Length == 2)
            {
                // 命令行参数模式
                string hexFile = args[0];
                string binFile = args[1];

                if (!File.Exists(hexFile))
                {
                    Console.WriteLine($"错误: 文件 {hexFile} 不存在");
                    return;
                }

                HexFileConverter.ConvertHexToBin(hexFile, binFile);
            }
            else
            {
                // 交互模式
                Console.Write("请输入 HEX 文件路径: ");
                string hexFile = Console.ReadLine();

                Console.Write("请输入输出 BIN 文件路径: ");
                string binFile = Console.ReadLine();

                if (!File.Exists(hexFile))
                {
                    Console.WriteLine($"错误: 文件 {hexFile} 不存在");
                    return;
                }

                Console.Write("请输入填充字节 (十六进制, 默认 FF): ");
                string fillByteStr = Console.ReadLine();
                byte fillByte = 0xFF;

                if (!string.IsNullOrEmpty(fillByteStr))
                {
                    fillByte = Convert.ToByte(fillByteStr, 16);
                }

                HexFileConverter.ConvertHexToBin(hexFile, binFile, fillByte);
            }

            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }
    }
}