# Introduction
Have you ever tried to check if you have write permissions on a certain path in C#? In windows this can become quite complex, because 

* Multiple rules apply to a single path (reasonably easy to query in C#),
* Only a subset of those rules applies to a given user (not trivial to achive in C#),
* One such rule can *allow* or *deny* certain rights, which further complicates things (you like flags bit logic?).

Effective permissions are the combination of the three above, that is

1.  Compute the allowed access rights from all rules relevant for the user
2.  Subtract all denied access rights from all rules relevant for the user

# Effective Permissions in C# #
Unfortunately C# does not provide a method to compute effective permissions and the Stackoverflow examples are either incomplete, not correct, or explicitely test for a specific permission. This repo has a single class ([AccessRights.cs](EffectivePermissions/EffectivePermissions/AccessRights.cs)) that handles the effective permissions on a certain path.

# Console Application
Furthermore, a small console applications checks the permission on the current folder, all contained files, and recurses down. Human readable output is written to a log file on the desktop. This is meant for your customers like "copy this exe to directory xy, run, and send me the log file from your desktop". This helped me to diagnose strange access problems on a customers network share.

