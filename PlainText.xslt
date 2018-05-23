<?xml version="1.0" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
<xsl:output method="text"/>

<xsl:template match="Error">

	<xsl:for-each select="Info">
		<xsl:value-of select="@message"/><xsl:text>&#10;</xsl:text>
		<xsl:value-of select="@type"/><xsl:text>&#10;</xsl:text>

		<xsl:if test="@Build">
			<xsl:value-of select="@Build"/><xsl:text>&#10;</xsl:text>
		</xsl:if>
		<xsl:if test="@SQL">
			<xsl:value-of select="@SQL"/><xsl:text>&#10;</xsl:text>
		</xsl:if>
		<xsl:text>&#10;</xsl:text><xsl:text>&#10;</xsl:text>

		<xsl:for-each select="Node[@id!='Library' and @id!='MemoryUsed' and @id!='Framework' and @id!='URI' and @id!='RequestHeaders']">
			<xsl:call-template name="node-row"/>
		</xsl:for-each>
	</xsl:for-each>
	<xsl:text>&#10;</xsl:text><xsl:text>&#10;</xsl:text>

	<xsl:for-each select="Exceptions">
		<xsl:call-template name="ex-block"/>
	</xsl:for-each>
	<xsl:text>&#10;</xsl:text>

	<xsl:for-each select="Info/Node[@id='Library' or @id='MemoryUsed' or @id='Framework' or @id='URI' or @id='RequestHeaders']">
		<xsl:call-template name="node-row"/>
	</xsl:for-each>
</xsl:template>


<xsl:template name="ex-block">
	<xsl:for-each select="Ex">
		<xsl:for-each select="Node">
			<xsl:call-template name="node-row"/>
		</xsl:for-each>
		<xsl:text>&#10;</xsl:text>
	</xsl:for-each>
</xsl:template>


<xsl:template name="node-row">
	<xsl:value-of select="@name"/> : <xsl:if test="@code='1'"><xsl:text>&#10;</xsl:text></xsl:if>
	<xsl:value-of select="@value"/>
	<xsl:text>&#10;</xsl:text>
</xsl:template>

</xsl:stylesheet>
