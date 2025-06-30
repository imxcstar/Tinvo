using Serilog;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Tinvo.Abstractions.AIScheduler;
using Tinvo.Application.DataStorage;

namespace Tinvo.Provider.Skill.Image
{
    public class ImageSkill
    {
        private readonly ILogger _logger;
        private readonly IDataStorageService _dataStorageService;

        public ImageSkill(IDataStorageService dataStorageService)
        {
            _logger = Log.ForContext<ImageSkill>();
            _dataStorageService = dataStorageService;
        }

        [Description("给图片添加文本水印")]
        public IAIChatHandleMessage ImageAddTextWatermark([Description("文件ID"), Required] string fileId, [Description("水印内容"), Required] string content)
        {
            var fileHas = _dataStorageService.ContainKeyAsync(fileId).Result;
            if (!fileHas)
                return new AIProviderHandleTextMessageResponse() { Message = "文件ID不存在" };
            var dataByte = _dataStorageService.GetItemAsBinaryAsync(fileId).Result;
            var memoryStream = new MemoryStream();
            using (Image<Rgba32> image = SixLabors.ImageSharp.Image.Load<Rgba32>(dataByte))
            {
                FontCollection fonts = new FontCollection();
                FontFamily fontFamily = fonts.Add("NotoSansSC-Regular.ttf");
                Font font = fontFamily.CreateFont(36, FontStyle.Bold);

                var textColor = Color.White.WithAlpha(0.5f);

                var textOptions = new RichTextOptions(font)
                {
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Origin = new PointF(image.Width - 20, image.Height - 20),
                    Dpi = 72
                };

                image.Mutate(ctx => ctx.DrawText(textOptions, content, textColor));

                image.SaveAsPng(memoryStream);
            }
            var newFileCustomId = Guid.NewGuid().ToString();
            dataByte = memoryStream.ToArray();
            _dataStorageService.SetItemAsBinaryAsync(newFileCustomId, dataByte);
            return new AIProviderHandleCustomFileMessageResponse()
            {
                Type = AIChatHandleMessageType.ImageMessage,
                FileCustomID = newFileCustomId,
            };
        }
    }
}
