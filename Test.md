---
layout: default
---
{%- if site.posts.size > 0 -%}
{%- assign post = site.posts | first -%}
{%- assign date_format = site.minima.date_format | default: "%b %-d, %Y" -%}
{{ post.date | date_format }}
{%- endif -%}
