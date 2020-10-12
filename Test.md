---
layout: default
---
{% for collection in site.collections %}
{{ collection.label }}
{{ collection.docs }}
{{ collection.files }}
{{ collection.relative_directory }}
{{ collection.directory }}
{{ collection.output }}
% endfor %}
