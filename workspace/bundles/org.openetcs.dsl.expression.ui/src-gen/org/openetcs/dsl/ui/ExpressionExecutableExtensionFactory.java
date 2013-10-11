/*
 * generated by Xtext
 */
package org.openetcs.dsl.ui;

import org.eclipse.xtext.ui.guice.AbstractGuiceAwareExecutableExtensionFactory;
import org.osgi.framework.Bundle;

import com.google.inject.Injector;

import org.openetcs.dsl.ui.internal.ExpressionActivator;

/**
 * This class was generated. Customizations should only happen in a newly
 * introduced subclass. 
 */
public class ExpressionExecutableExtensionFactory extends AbstractGuiceAwareExecutableExtensionFactory {

	@Override
	protected Bundle getBundle() {
		return ExpressionActivator.getInstance().getBundle();
	}
	
	@Override
	protected Injector getInjector() {
		return ExpressionActivator.getInstance().getInjector(ExpressionActivator.ORG_OPENETCS_DSL_EXPRESSION);
	}
	
}