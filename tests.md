---
layout: page
title: Tests
---

{% assign releases = site.github.releases | where_exp: "item", "item.prerelease == 'false' and item.draft == 'false'" %}
{% for release in releases %}

  {{ release.published_at }}

  ##{{ release.name }}

  {{ release.body }}

  Download: {% assign asset = site.github.latest_release.assets | first %}{{ asset.browser_download_url }}
  Release: {{ release.html_url }}

{% endfor %}
