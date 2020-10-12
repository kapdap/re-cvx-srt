---
layout: default
---
{% for collection in site.collections %}
{{ collection.label }}
{% endfor %}
