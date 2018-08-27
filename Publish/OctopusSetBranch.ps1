<#
    ## CODE OWNERS: Ben Wyatt, Steve Gredell

    ### OBJECTIVE:
		Set a variable to be used elsewhere in Octopus deployments

    ### DEVELOPER NOTES:
        This should only be run in non-production deployments
#>

$releaseName = $OctopusParameters['Octopus.Release.Number']

$branch = $releaseName.Substring($releaseName.IndexOf('-') + 1)

write-output "Setting BRANCH_NAME to $branch"

Set-OctopusVariable -name "BRANCH_NAME" -value $branch

Set-OctopusVariable -name "BRANCH_NAME_TRIMMED" -value $branch.Replace("_","").Replace("-","").ToLower()