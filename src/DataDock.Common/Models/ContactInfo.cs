using Newtonsoft.Json;

namespace DataDock.Common.Models
{
    /// <summary>
    /// Person / Organisation / Entity Contact Details
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class ContactInfo
    {
        /// <summary>
        /// Type e.g. Organisation or Individual
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// Publisher Name
        /// </summary>
        [JsonProperty("label")]
        public string Label { get; set; }


        /// <summary>
        /// Publisher Contact Email
        /// </summary>
        [JsonProperty("email")]
        public string Email { get; set; }

        /// <summary>
        /// Publisher Contact Email
        /// </summary>
        [JsonProperty("website")]
        public string Website { get; set; }

        public bool IsDisplayable()
        {
            return !string.IsNullOrEmpty(Label) || !string.IsNullOrEmpty(Email) || !string.IsNullOrEmpty(Website);
        }
    }
}
