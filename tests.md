---
layout: page
title: Tests
---

{% assign releases = site.github.releases | where: "prerelease" %}
{% for release in releases %}

  {{ release.published_at }}

  ##{{ release.name }}

  {{ release.body }}
  
  Release: {{ release.html_url }}

{% endfor %}
