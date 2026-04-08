{{- define "training.hosts.httpapi" -}}
{{- print "https://" (.Values.global.hosts.httpapi | replace "[RELEASE_NAME]" .Release.Name) -}}
{{- end -}}
{{- define "training.hosts.angular" -}}
{{- print "https://" (.Values.global.hosts.angular | replace "[RELEASE_NAME]" .Release.Name) -}}
{{- end -}}
