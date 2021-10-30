using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace AspDemos.Helpers {
    [HtmlTargetElement("a")]
    public class OpenModalTagHelper : TagHelper {
        private const string ModalAttibuteName = "open-modal";

        [HtmlAttributeName(ModalAttibuteName)]
        public bool attrValue { get; set; }
        public override void Process(TagHelperContext context, TagHelperOutput output) {
            if (attrValue) {
                output.Attributes.SetAttribute("onclick", "pageModal(event)");
            }
        }
    }

    [HtmlTargetElement("a")]
    public class CloseModalTagHelper : TagHelper {
        private const string ModalAttibuteName = "close-modal";

        [ViewContext]
        public ViewContext ViewContext { get; set; }

        [HtmlAttributeName(ModalAttibuteName)]
        public bool attrValue { get; set; }
        public override void Process(TagHelperContext context, TagHelperOutput output) {
            if (attrValue && ViewContext.HttpContext.Request.Query["modalView"] == "true") {
                output.Attributes.SetAttribute("data-bs-dismiss", "modal");
                output.Attributes.SetAttribute("href", "");
            }
        }
    }
}
