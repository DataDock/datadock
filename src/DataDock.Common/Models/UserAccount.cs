using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Nest;

namespace DataDock.Common.Models
{
    [ElasticsearchType(Name = "useraccount", IdProperty = "UserId")]
    public class UserAccount
    {
        /// <summary>
        /// Get or set the user identifier for the account
        /// </summary>
        [Keyword]
        public string UserId { get; set; }

        /// <summary>
        /// Get or set the claims connected with this account
        /// </summary>
        [Object]
        public List<AccountClaim> AccountClaims { get; set; }


        /// <summary>
        /// Provides a way of getting/setting the account claims as System.Security.Claim instances
        /// </summary>
        [Ignore]
        public IEnumerable<Claim> Claims
        {
            get { return AccountClaims.Where(c=>!(string.IsNullOrEmpty(c.Type) || string.IsNullOrEmpty(c.Value))).Select(c => new Claim(c.Type, c.Value)); }
            set { AccountClaims = value.Select(c => new AccountClaim(c.Type, c.Value)).ToList(); }
        }

    }
}
