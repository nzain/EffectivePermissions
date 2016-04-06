using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;

namespace EffectivePermissions
{
    /// <summary>
    /// Represents effective <see cref="System.Security.AccessControl.FileSystemRights" /> for a user on a certain path.
    /// To understand how those are computed, check the private methods at the end of this class.
    /// </summary>
    public class AccessRights
    {
        /// <summary>Constructs the effective permissions for the given <c>path</c>.</summary>
        /// <param name="path">The path to check.</param>
        /// <param name="user">The user to compute effective permissions for.</param>
        public AccessRights(string path, WindowsPrincipal user)
        {
            if (!Directory.Exists(path) && !File.Exists(path))
            {
                throw new FileNotFoundException(path);
            }
            this.Path = path;
            this.ApplicableRules = new List<FileSystemAccessRule>();
            this.IrrelevantRules = new List<FileSystemAccessRule>();
            try
            {
                this.GetAccessRules(user);
                this.FileSystemRights = this.ComputeEffectivePermissions();
            }
            catch (Exception e)
            {
                // e.g. UnauthorizedAccessException if you can't even query ACL infos!
                // try with "c:\\system volume information"
                this.Exception = e;
            }
        }

        /// <summary>Gets the path this instance refers to.</summary>
        public string Path { get; }

        /// <summary>Gets the effective permissions on this <c>Path</c>.</summary>
        public FileSystemRights FileSystemRights { get; }

        /// <summary>Checks the <see cref="System.Security.AccessControl.FileSystemRights.Read" /> flag.</summary>
        public bool CanRead => this.FileSystemRights.HasFlag(FileSystemRights.Read);

        /// <summary>Checks the <see cref="System.Security.AccessControl.FileSystemRights.Write" /> flag.</summary>
        public bool CanWrite => this.FileSystemRights.HasFlag(FileSystemRights.Write);

        /// <summary>Checks the <see cref="System.Security.AccessControl.FileSystemRights.ReadAndExecute" /> flag.</summary>
        public bool CanExecute => this.FileSystemRights.HasFlag(FileSystemRights.ReadAndExecute);

        /// <summary>Gets the access rules that apply to the given user.</summary>
        /// <see cref="IrrelevantRules" />
        public List<FileSystemAccessRule> ApplicableRules { get; }

        /// <summary>Gets the access rules that do NOT apply to the given user.</summary>
        /// <see cref="ApplicableRules" />
        public List<FileSystemAccessRule> IrrelevantRules { get; }

        /// <summary>Gets the "allow" rules applicable to the given user.</summary>
        /// <see cref="ApplicableRules" />
        public IEnumerable<FileSystemAccessRule> ApplicableAllowRules
            => this.ApplicableRules.Where(w => w.AccessControlType == AccessControlType.Allow);

        /// <summary>
        ///     Gets the "deny" rules applicable to the given user. Often empty, but complex company structures may require
        ///     such rules.
        /// </summary>
        public IEnumerable<FileSystemAccessRule> ApplicableDenyRules
            => this.ApplicableRules.Where(w => w.AccessControlType == AccessControlType.Deny);

        /// <summary>Gets the exception, if any. This typically means that you cannot even query the ACL infos on this <c>Path</c>.</summary>
        public Exception Exception { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Path  : {this.Path}");
            if (this.Exception != null)
            {
                sb.AppendLine(this.Exception.ToString());
                return sb.ToString();
            }
            sb.AppendLine($"Rights: {this.FileSystemRights}");
            foreach (FileSystemAccessRule rule in this.IrrelevantRules)
            {
                sb.AppendLine($"  [ ] {rule.IdentityReference}");
            }
            foreach (FileSystemAccessRule rule in this.ApplicableAllowRules)
            {
                sb.AppendLine($"  [x] {rule.IdentityReference,-40} +{rule.FileSystemRights}");
            }
            foreach (FileSystemAccessRule rule in this.ApplicableDenyRules)
            {
                sb.AppendLine($"  [x] {rule.IdentityReference,-40} -{rule.FileSystemRights}");
            }
            return sb.ToString();
        }

        #region Compute Effective permissions

        private void GetAccessRules(WindowsPrincipal currentuser)
        {
            FileSystemSecurity security = File.GetAccessControl(this.Path);
            AuthorizationRuleCollection rules = security.GetAccessRules(true, true, typeof (NTAccount));
            foreach (FileSystemAccessRule rule in rules)
            {
                if (currentuser.IsInRole(rule.IdentityReference.Value))
                {
                    this.ApplicableRules.Add(rule);
                }
                else if (rule.IdentityReference.Value.StartsWith("S-1-"))
                {
                    var sid = new SecurityIdentifier(rule.IdentityReference.Value);
                    if (currentuser.IsInRole(sid))
                    {
                        this.ApplicableRules.Add(rule);
                    }
                }
                else
                {
                    this.IrrelevantRules.Add(rule);
                }
            }
        }

        private FileSystemRights ComputeEffectivePermissions()
        {
            // 1. nothing allowed initially (flags enum, all bits zero)
            FileSystemRights rights = 0;

            // 2. add all allowed rights
            foreach (FileSystemAccessRule rule in this.ApplicableAllowRules)
            {
                rights |= rule.FileSystemRights;
            }

            // 3. subtract all denied rights
            foreach (FileSystemAccessRule rule in this.ApplicableDenyRules)
            {
                rights &= ~rule.FileSystemRights;
            }
            return rights;
        }

        #endregion
    }
}