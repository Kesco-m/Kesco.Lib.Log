<?xml version="1.0" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
<xsl:output method="html"/>

<xsl:template match="Error">
<html>
	<head>
		<style>
			body, table{font-size: 9pt;font-family: Verdana;}
			h1 {color:#800000;font-size: 12pt;font-weight: bold;}
			h2 {font-size: 10pt;font-weight: bold;}
			.error {font-weight: bold; color: red;padding:10px 0;}
			td.head {vertical-align:top;padding-right:10px;width:110px;}
			table#CommonInfo tr td.head {font-weight: bold;}
			pre {background-color: #FFFFCC;margin:5px 0;}
		</style>
	</head>
	<body>

		<xsl:for-each select="Info">
			<h1><xsl:value-of select="@message"/></h1>
			<h2><xsl:value-of select="@type"/></h2>

			<xsl:if test="@Build">
				<div class="error"><xsl:value-of select="@Build"/></div>
			</xsl:if>
			<xsl:if test="@SQL">
				<div class="error"><xsl:value-of select="@SQL"/></div>
			</xsl:if>
			<hr size="1" width="100%"/>

			<table id="CommonInfo" cellspacing="0" cellpadding="0" width="100%">
				<xsl:for-each select="Node[@id!='Library' and @id!='MemoryUsed' and @id!='Framework' and @id!='URI' and @id!='RequestHeaders']">
					<xsl:call-template name="node-row"/>
				</xsl:for-each>
			</table>
			<br/><hr size="1" width="100%"/>
		</xsl:for-each>

		<xsl:for-each select="Exceptions">
			<xsl:call-template name="ex-block"/>
		</xsl:for-each>

		<table cellspacing="0" cellpadding="0" width="100%">
			<xsl:for-each select="Info/Node[@id='Library' or @id='MemoryUsed' or @id='Framework' or @id='URI' or @id='RequestHeaders']">
				<xsl:call-template name="node-row"/>
			</xsl:for-each>
		</table>
	</body>
</html>
</xsl:template>


<xsl:template name="ex-block">
	<table id="trace" cellspacing="0" cellpadding="0" width="100%">
		<xsl:for-each select="Ex">
			<xsl:for-each select="Node">
				<xsl:call-template name="node-row"/>
			</xsl:for-each>
			<tr><td colspan="2">&#160;</td></tr>
		</xsl:for-each>
	</table>
</xsl:template>


<xsl:template name="node-row">
	<tr id="{@id}">
		<td class="head" nowrap="nowrap"><xsl:value-of select="@name"/></td>
		<td>
			<xsl:choose>
				<xsl:when test="@code">
					<pre>
<xsl:value-of select="@value"/>
					</pre>
				</xsl:when>
				<xsl:otherwise>
					<xsl:value-of select="@value"/>
				</xsl:otherwise>
			</xsl:choose>
		</td>
	</tr>
</xsl:template>

</xsl:stylesheet>
